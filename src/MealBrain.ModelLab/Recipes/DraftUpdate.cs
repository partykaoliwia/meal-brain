using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace MealBrain.ModelLab.Recipes;

// Propozycja zmian zwracana przez model. To tylko propozycja: stosuje ją DraftUpdateApplier przez DraftSession.
public sealed record DraftUpdateProposal
{
    public required string Intent { get; init; }
    public required DraftUpdateAction[] Actions { get; init; }
    public required string? Clarification { get; init; }
}

public sealed record DraftUpdateAction
{
    public required string Type { get; init; }
    public required string? IngredientId { get; init; }
    public required string? ConflictId { get; init; }
    public required int? Servings { get; init; }
    public required AmountDraft? Amount { get; init; }
    public required MinuteRange? Minutes { get; init; }
    public required int? ReviewIssue { get; init; }
}

public sealed record InterpretationResult(DraftUpdateProposal? Proposal, string? Error, long ElapsedMs);

public sealed record TurnOutcome(string Intent, DraftUpdateResult[] Results, string? Clarification);

// Wysyła do modelu: aktualny szkic, braki, uwagi i ostatnią wypowiedź. Bez historii rozmowy.
public sealed class DraftUpdateInterpreter(HttpClient http, string model, string prompt, JsonElement schema,
    Func<object, Task> log)
{
    public const string PromptVersion = "draft-update-prompt-v1";

    public static string BuildContext(DraftSession session, string userText)
    {
        var validation = session.Validation;
        var issues = session.OpenReviewIssues;
        return $"""
            Aktualny szkic (rewizja {session.Revision}, stan {session.State}):
            {JsonSerializer.Serialize(session.Document, RecipeJson.Options)}

            Braki:
            {(validation.MissingFields.Length == 0 ? "brak" : string.Join("\n", validation.MissingFields.Select(m => "- " + m)))}

            Uwagi do przeglądu:
            {(issues.Length == 0 ? "brak" : string.Join("\n", issues.Select((issue, i) => $"{i + 1}. {issue}")))}

            Wypowiedź użytkowniczki:
            {userText}
            """;
    }

    public async Task<InterpretationResult> InterpretAsync(DraftSession session, string userText, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model,
            messages = new[]
            {
                new Message("system", prompt + "\n\nSchemat odpowiedzi:\n" + schema.GetRawText()),
                new Message("user", BuildContext(session, userText))
            },
            stream = false,
            think = false,
            format = schema,
            options = new { temperature = 0, num_ctx = 8192, num_predict = 1024 },
            keep_alive = "10m"
        };
        var watch = Stopwatch.StartNew();
        string? raw = null;
        try
        {
            using var response = await http.PostAsJsonAsync("/api/chat", request, cancellationToken);
            raw = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(raw);
            var root = json.RootElement;
            if (!root.TryGetProperty("message", out var message) || !message.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("Ollama nie zwróciła tekstowej odpowiedzi.");
            if (!root.TryGetProperty("done_reason", out var reason) || reason.GetString() != "stop")
                throw new InvalidDataException("Odpowiedź Ollamy jest niekompletna lub osiągnęła limit tokenów.");
            watch.Stop();
            DraftUpdateProposal? proposal = null;
            string? error = null;
            try
            {
                proposal = JsonSerializer.Deserialize<DraftUpdateProposal>(content.GetString()!, RecipeJson.Options);
                if (proposal is null) error = "Pusta propozycja.";
            }
            catch (JsonException ex) { error = "Odpowiedź nie pasuje do DraftUpdateProposal: " + ex.Message; }
            await log(new { kind = "interpret", prompt_version = PromptVersion, revision = session.Revision,
                elapsed_ms = watch.ElapsedMilliseconds, user_text = userText, request, response = root, proposal, error });
            return new(proposal, error, watch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
        {
            watch.Stop();
            await log(new { kind = "error", mode = "interpret", elapsed_ms = watch.ElapsedMilliseconds,
                user_text = userText, raw_response = raw, error = ex.Message });
            throw;
        }
    }
}

// Stosuje propozycję modelu. Każda akcja przechodzi przez DraftSession, więc błędna propozycja nie psuje szkicu.
public static class DraftUpdateApplier
{
    public static TurnOutcome Apply(DraftSession session, DraftUpdateProposal proposal, string userText)
    {
        var results = new List<DraftUpdateResult>();
        switch (proposal.Intent)
        {
            case "update":
                if (proposal.Actions.Length == 0) results.Add(DraftUpdateResult.Rejected("Model nie zaproponował żadnej zmiany."));
                // Numery uwag odnoszą się do listy sprzed tej tury, więc ją zapamiętujemy.
                var issues = session.OpenReviewIssues;
                foreach (var action in proposal.Actions) results.Add(ApplyAction(session, action, issues, userText));
                break;
            case "approve":
                // Zgoda zawsze dotyczy rewizji, którą użytkowniczka widziała na początku tej tury.
                results.Add(session.Approve(session.Revision, userText));
                break;
            case "decline":
                results.Add(DraftUpdateResult.Ok("Szkic pozostaje niezatwierdzony."));
                break;
        }
        if (proposal.Intent != "update" && proposal.Actions.Length > 0)
            results.Add(DraftUpdateResult.Rejected($"Zignorowano akcje przy intencji {proposal.Intent}."));
        return new(proposal.Intent, results.ToArray(), proposal.Clarification);
    }

    private static DraftUpdateResult ApplyAction(DraftSession session, DraftUpdateAction action, string[] issues, string userText)
    {
        var conflict = action.ConflictId is null ? null : session.Recipe?.Conflicts.FirstOrDefault(c => c.Id == action.ConflictId);
        return action.Type switch
        {
            "setServings" when action.Servings is { } servings => session.SetServings(servings, userText),
            "setTotalTime" when action.Minutes is { } minutes => session.SetTotalTime(minutes, userText),
            "setIngredientAmount" when action.IngredientId is { } id && action.Amount is { } amount =>
                session.SetIngredientAmount(id, amount, userText),
            "resolveConflict" when conflict is null => DraftUpdateResult.Rejected($"Nie ma konfliktu {action.ConflictId}."),
            "resolveConflict" when conflict.Target.Type == "ingredientAmount" && action.Amount is { } amount =>
                session.ResolveAmountConflict(conflict.Id, amount, userText),
            "resolveConflict" when conflict.Target.Type == "servings" && action.Servings is { } servings =>
                session.ResolveServingsConflict(conflict.Id, servings, userText),
            "resolveConflict" when action.Minutes is { } minutes => session.ResolveTimeConflict(conflict.Id, minutes, userText),
            "acknowledgeReview" when action.ReviewIssue is { } n && n >= 1 && n <= issues.Length =>
                session.AcknowledgeReviewIssue(issues[n - 1], userText),
            _ => DraftUpdateResult.Rejected($"Akcja {action.Type} nie ma wymaganych pól.")
        };
    }
}
