using System.Text.Json;

namespace MealBrain.ModelLab.Recipes;

// Rozmowa na zapisanym szkicu: model proponuje zmiany, DraftSession je sprawdza i stosuje.
public static class DraftConsole
{
    // Gotowe szkice z regresji S3 (handoff Oliwii): ryż, ogórki, kanapki, sos.
    private static readonly Dictionary<string, (int File, string Name)> Cases = new()
    {
        ["ryz"] = (1, "ryż — kompletny"),
        ["ogorki"] = (2, "ogórki — dwa braki"),
        ["kanapki"] = (3, "kanapki — zmieniony cytat"),
        ["sos"] = (6, "sos — konflikt 20/30 ml")
    };

    public static async Task<int> RunAsync(string model, TextReader? input = null, HttpClient? httpClient = null, string? runRoot = null)
    {
        input ??= Console.In;
        var prompt = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "draft-update-prompt-v1.txt"));
        var schema = JsonSerializer.Deserialize<JsonElement>(
            await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "draft-update-v1.schema.json")));
        var directory = Path.GetFullPath(Path.Combine(runRoot ?? "runs", $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-draft-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(directory);
        var logPath = Path.Combine(directory, "turns.jsonl");
        using var ownedHttp = httpClient is null
            ? new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(10) }
            : null;
        var http = httpClient ?? ownedHttp!;
        string? caseId = null;
        Task Log(object entry) => File.AppendAllTextAsync(logPath,
            JsonSerializer.Serialize(new { case_id = caseId, at_utc = DateTimeOffset.UtcNow, entry }) + Environment.NewLine);
        var interpreter = new DraftUpdateInterpreter(http, model, prompt, schema, Log);

        Console.WriteLine($"Rozmowa na szkicu ({DraftUpdateInterpreter.PromptVersion}), model {model}. Logi: {directory}");
        Console.WriteLine("Komendy: /case ryz|ogorki|kanapki|sos, /show, /history, /exit. Inny tekst trafia do modelu.");
        DraftSession? session = null;
        while (true)
        {
            Console.Write("> ");
            var line = input.ReadLine();
            if (line is null || line.Trim() == "/exit") break;
            line = line.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("/case"))
            {
                var name = line["/case".Length..].Trim();
                if (!Cases.TryGetValue(name, out var item)) { Console.WriteLine("Nieznany przypadek. Dostępne: " + string.Join(", ", Cases.Keys)); continue; }
                var prefix = Path.Combine(AppContext.BaseDirectory, "drafts", $"extract-{item.File:000}");
                var source = await File.ReadAllTextAsync(prefix + ".source.txt");
                session = new DraftSession(RecipeJson.Parse(await File.ReadAllTextAsync(prefix + ".draft.json")), source);
                caseId = name;
                await Log(new { kind = "case", name, file = $"extract-{item.File:000}" });
                Console.WriteLine($"Wczytano: {item.Name}\nŹródło:\n{source.Trim()}\n");
                Print(session);
                continue;
            }
            if (session is null) { Console.WriteLine("Najpierw wybierz przypadek: /case ogorki"); continue; }
            if (line == "/show") { Print(session); continue; }
            if (line == "/history") { PrintHistory(session); continue; }

            InterpretationResult interpreted;
            try { interpreted = await interpreter.InterpretAsync(session, line); }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
            {
                Console.WriteLine($"Błąd połączenia z modelem: {ex.Message}. Szkic bez zmian.");
                continue;
            }
            if (interpreted.Proposal is null)
            {
                Console.WriteLine($"Model zwrócił niepoprawną propozycję ({interpreted.Error}). Szkic bez zmian.");
                continue;
            }
            var outcome = DraftUpdateApplier.Apply(session, interpreted.Proposal, line);
            await Log(new { kind = "apply", outcome, revision = session.Revision, state = session.State.ToString() });
            Console.WriteLine($"[model: {outcome.Intent}, {interpreted.ElapsedMs} ms]");
            foreach (var result in outcome.Results) Console.WriteLine((result.Applied ? "  ✓ " : "  ✗ ") + result.Message);
            if (outcome.Clarification is not null) Console.WriteLine("Pytanie: " + outcome.Clarification);
            if (outcome.Intent is "update" or "show") Print(session);
            else Console.WriteLine($"Stan: {session.State}, rewizja {session.Revision}.");
        }
        Console.WriteLine($"Koniec. Logi: {logPath}");
        return 0;
    }

    public static void Print(DraftSession session)
    {
        var recipe = session.Recipe;
        if (recipe is null) { Console.WriteLine($"To nie jest przepis: {session.Document.RejectionReason}"); return; }
        Console.WriteLine($"— {recipe.Title ?? "(bez tytułu)"} — rewizja {session.Revision}, stan {session.State}");
        Console.WriteLine($"Porcje: {recipe.Servings?.ToString() ?? "?"}   Czas całkowity: {DraftSession.Describe(recipe.Times.Total) ?? "?"}");
        foreach (var i in recipe.Ingredients)
            Console.WriteLine($"  {i.Id}: {i.Name} — {DraftSession.Describe(i.Amount)}{(i.Optional ? " (opcjonalnie)" : "")}");
        foreach (var s in recipe.Steps) Console.WriteLine($"  {s.Id}. {s.Text}");
        foreach (var c in recipe.Conflicts)
            Console.WriteLine($"  Konflikt {c.Id} ({c.Target.Type} {c.Target.Id}): {string.Join(" / ", c.Candidates.Select(x => x.ValueText))}");
        var validation = session.Validation;
        foreach (var m in validation.MissingFields) Console.WriteLine("  Brak: " + m);
        var issues = session.OpenReviewIssues;
        for (var n = 0; n < issues.Length; n++) Console.WriteLine($"  Do przeglądu {n + 1}: {issues[n]}");
        if (session.State == DraftState.ReadyForReview) Console.WriteLine("  Szkic kompletny. Czy zatwierdzasz tę wersję?");
    }

    private static void PrintHistory(DraftSession session)
    {
        if (session.History.Count == 0) { Console.WriteLine("Brak zmian."); return; }
        foreach (var h in session.History)
            Console.WriteLine($"  r{h.Revision} {h.Operation} {h.Target}: {h.OldValue ?? "-"} → {h.NewValue ?? "-"}  («{h.UserText}»)");
    }
}
