using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace MealBrain.ModelLab.Recipes;

public sealed record ExtractionResult(RecipeExtractionDocument? Document, DraftValidation Validation, long ElapsedMs);

public sealed class RecipeExtractor(HttpClient http, string model, string prompt, JsonElement schema,
    Func<object, Task> log)
{
    public async Task<ExtractionResult> ExtractRecipeAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Tekst przepisu jest pusty.", nameof(text));
        var request = new
        {
            model,
            messages = new[]
            {
                new Message("system", prompt + "\n\nSchemat odpowiedzi:\n" + schema.GetRawText()),
                new Message("user", text)
            },
            stream = false,
            think = false,
            format = schema,
            options = new { temperature = 0, num_ctx = 8192, num_predict = 4096 },
            keep_alive = "10m"
        };
        var watch = Stopwatch.StartNew();
        var startedAt = DateTimeOffset.UtcNow;
        string? raw = null;
        try
        {
            using var response = await http.PostAsJsonAsync("/api/chat", request, cancellationToken);
            raw = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(raw);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object ||
                !message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(content.GetString()))
                throw new InvalidDataException("Ollama nie zwróciła tekstowej odpowiedzi końcowej.");
            if (!root.TryGetProperty("done", out var done) || done.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("done_reason", out var reason) || reason.ValueKind != JsonValueKind.String || reason.GetString() != "stop")
                throw new InvalidDataException("Odpowiedź Ollamy jest niekompletna lub osiągnęła limit tokenów.");
            watch.Stop();
            RecipeExtractionDocument? document = null;
            DraftValidation validation;
            try
            {
                document = RecipeJson.Parse(content.GetString()!);
                validation = RecipeDraftValidator.Validate(document, text);
            }
            catch (JsonException ex)
            {
                validation = new("invalid_output", [$"Odpowiedź nie odpowiada klasom RecipeDraft: {ex.Message}"], [], []);
            }
            await log(new { kind = "extract", started_at_utc = startedAt,
                elapsed_ms = watch.ElapsedMilliseconds, request, response = root, validation });
            return new(document, validation, watch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
        {
            watch.Stop();
            await log(new { kind = "error", mode = "extract", started_at_utc = startedAt,
                elapsed_ms = watch.ElapsedMilliseconds, request, raw_response = raw, error = ex.Message });
            throw;
        }
    }
}
