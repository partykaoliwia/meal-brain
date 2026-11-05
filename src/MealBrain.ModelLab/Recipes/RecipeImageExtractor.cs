using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace MealBrain.ModelLab.Recipes;

public sealed record ImageExtractionOptions(double Temperature = 0, int NumCtx = 16384, int NumPredict = 4096)
{
    public void Validate()
    {
        if (!double.IsFinite(Temperature) || Temperature is < 0 or > 2 || NumCtx < 1024 || NumPredict < 1)
            throw new InvalidDataException("Nieprawidłowe ustawienia odczytu obrazu.");
    }
}

public sealed record RecipeImage(byte[] Bytes, string Extension, string Sha256)
{
    public static RecipeImage Load(string path)
    {
        const int maxBytes = 25 * 1024 * 1024;
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException("Brak obrazu: " + path, path);
        if (file.Length is < 1 or > maxBytes)
            throw new InvalidDataException("Obraz musi mieć od 1 bajta do 25 MB: " + path);
        var bytes = File.ReadAllBytes(path);
        var extension = bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
            : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg"
            : throw new InvalidDataException("Obsługiwane są pliki PNG i JPEG: " + path);
        return new(bytes, extension, Convert.ToHexString(SHA256.HashData(bytes)));
    }
}

// Ten sam RecipeDraft co w S3. Odczyt sourceText z obrazu nadal wymaga oceny człowieka.
public sealed class RecipeImageExtractor(HttpClient http, string model, string prompt, JsonElement schema,
    ImageExtractionOptions options, Func<object, Task> log)
{
    public const string UserInstruction = "Odczytaj przepis z załączonego obrazu zgodnie ze schematem. Nie uzupełniaj niewidocznych danych.";

    public async Task<ExtractionResult> ExtractRecipeAsync(RecipeImage image, CancellationToken cancellationToken = default)
    {
        options.Validate();
        var messages = new object[]
        {
            new { role = "system", content = prompt + "\n\nSchemat odpowiedzi:\n" + schema.GetRawText() },
            new { role = "user", content = UserInstruction, images = new[] { Convert.ToBase64String(image.Bytes) } }
        };
        // Nie wysyłamy think=false: Gemma 3 i wariant Qwen Instruct nie wymagają tego pola.
        var request = new { model, messages, stream = false, format = schema,
            options = new { temperature = options.Temperature, num_ctx = options.NumCtx, num_predict = options.NumPredict },
            keep_alive = "10m" };
        // Bajty źródła są zachowane osobno. W logu nie powielamy dużego base64.
        var requestEvidence = new { model, user_instruction = UserInstruction, image_sha256 = image.Sha256,
            image_bytes = image.Bytes.Length, image_payload_omitted = true, stream = false, format = schema,
            options = request.options, keep_alive = request.keep_alive };
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
                // Nazwa pliku, base64 i ręczny wzorzec nie są oryginalnym tekstem obrazu.
                validation = RecipeDraftValidator.Validate(document);
            }
            catch (JsonException ex)
            {
                validation = new("invalid_output", ["Niepoprawny RecipeDraft: " + ex.Message], [], []);
            }
            await log(new { kind = "image_extract", started_at_utc = startedAt, elapsed_ms = watch.ElapsedMilliseconds,
                request = requestEvidence, response = root, validation, visual_review_required = true });
            return new(document, validation, watch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException)
        {
            watch.Stop();
            await log(new { kind = "error", mode = "image_extract", started_at_utc = startedAt,
                elapsed_ms = watch.ElapsedMilliseconds, request = requestEvidence, raw_response = raw, error = ex.Message });
            throw;
        }
    }
}
