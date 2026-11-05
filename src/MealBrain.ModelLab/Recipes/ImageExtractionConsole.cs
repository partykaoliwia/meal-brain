using System.Net.Http.Json;
using System.Text.Json;

namespace MealBrain.ModelLab.Recipes;

public sealed record ImageReviewRow(string CaseId, bool TechnicalCompleted, string? ValidationStatus,
    long? ElapsedMs, string? Error, bool VisualReviewRequired = true, bool? OcrPass = null,
    bool? InterpretationPass = null, bool? CriticalError = null, bool? Passed = null, string? Notes = null);

public static class ImageExtractionConsole
{
    public static string PromptPath => Path.Combine(AppContext.BaseDirectory, "image-extraction-prompt-v1.txt");
    public static string SchemaPath => Path.Combine(AppContext.BaseDirectory, "recipe-draft-v1.schema.json");

    public static int Prepare(string command, string manifestPath)
    {
        var suite = ImageEvaluationSuite.Load(manifestPath);
        if (command == "--check-image-suite")
        {
            var issues = suite.CheckFiles(manifestPath);
            foreach (var issue in issues) Console.Error.WriteLine(issue);
            Console.WriteLine($"Zestaw {suite.Id}: {suite.Cases.Length} obrazów; uwagi: {issues.Length}. Nie wywołano Ollamy.");
            return issues.Length == 0 ? 0 : 1;
        }
        var path = suite.Freeze(manifestPath, File.ReadAllText(PromptPath), File.ReadAllText(SchemaPath));
        Console.WriteLine("Zamrożono warunki: " + path + ". Nie wywołano Ollamy.");
        return 0;
    }

    public static async Task<int> RunAsync(string model, string inputPath, bool isSuite,
        HttpClient? httpClient = null, string? runRoot = null)
    {
        var prompt = await File.ReadAllTextAsync(PromptPath);
        var schemaText = await File.ReadAllTextAsync(SchemaPath);
        var schema = JsonSerializer.Deserialize<JsonElement>(schemaText);
        var manifestPath = Path.GetFullPath(inputPath);
        var suite = isSuite ? ImageEvaluationSuite.Load(manifestPath) : null;
        var frozen = suite?.VerifyFrozen(manifestPath, prompt, schemaText);
        var cases = suite?.Cases ?? [new ImageEvaluationCase("PILOT-001", manifestPath, "")];
        // Walidujemy cały zestaw, zanim wywołamy pierwszy model. W pamięci zachowujemy dokładne wejścia.
        var inputs = cases.Select(item => (Item: item, Image: RecipeImage.Load(suite is null
            ? manifestPath : ImageEvaluationSuite.Resolve(manifestPath, item.Image)))).ToArray();
        if (frozen is not null && inputs.Where((entry, index) => entry.Image.Sha256 != frozen.Cases[index].ImageSha256).Any())
            throw new InvalidDataException("Obraz zmienił się podczas przygotowania próby.");
        var options = suite?.Options ?? new ImageExtractionOptions();
        var directory = Path.GetFullPath(Path.Combine(runRoot ?? "runs", $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-s1-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "prompt.txt"), prompt);
        await File.WriteAllTextAsync(Path.Combine(directory, "schema.json"), schemaText);
        if (suite is not null)
        {
            File.Copy(manifestPath, Path.Combine(directory, "suite.json"));
            File.Copy(ImageEvaluationSuite.LockPath(manifestPath), Path.Combine(directory, "protocol.lock.json"));
        }
        var review = new List<ImageReviewRow>();
        await WriteReview();
        using var ownedHttp = httpClient is null
            ? new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(10) }
            : null;
        var http = httpClient ?? ownedHttp!;
        Console.WriteLine($"S1 — {(isSuite ? "zamrożony zestaw" : "próba pojedynczego obrazu")}, model: {model}");
        Console.WriteLine("Wyniki: " + directory);
        try
        {
            var version = await http.GetFromJsonAsync<JsonElement>("/api/version");
            var installed = await http.GetFromJsonAsync<JsonElement>("/api/tags");
            using var showResponse = await http.PostAsJsonAsync("/api/show", new { model });
            showResponse.EnsureSuccessStatusCode();
            var show = await showResponse.Content.ReadFromJsonAsync<JsonElement>();
            await File.WriteAllTextAsync(Path.Combine(directory, "metadata.json"), JsonSerializer.Serialize(new
            {
                mode = "image_extract", variant = "s1-images-v1", suite_id = suite?.Id,
                started_at_utc = DateTimeOffset.UtcNow, model, options, keep_alive = "10m", think_parameter = "omitted",
                schema_version = 1, validation_version = RecipeDraftValidator.Version,
                prompt_sha256 = ImageEvaluationSuite.HashText(prompt), schema_sha256 = ImageEvaluationSuite.HashText(schemaText),
                ollama_version = version, installed_models = installed, model_details = show,
                machine = Environment.MachineName, runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                visual_review_required = true, automatic_quality_grading = false,
                timing = "End-to-end /api/chat time; no warmup. First call may include model loading.",
                inputs = inputs.Select(entry => new { entry.Item.Id, entry.Image.Sha256, bytes = entry.Image.Bytes.Length })
            }, RecipeJson.Options));
            if (show.ValueKind != JsonValueKind.Object || !show.TryGetProperty("capabilities", out var capabilities) ||
                capabilities.ValueKind != JsonValueKind.Array || !capabilities.EnumerateArray().Any(c => c.ValueKind == JsonValueKind.String && c.GetString() == "vision"))
                throw new InvalidDataException("Model nie zgłasza obsługi vision. Użyj gemma3:4b lub qwen3-vl:4b-instruct i aktualnej Ollamy.");
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException)
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "error.txt"), ex.ToString());
            Console.Error.WriteLine("Nie można przygotować modelu: " + ex.Message + ". Sprawdź uruchomienie Ollamy i ollama list.");
            return 1;
        }

        foreach (var (item, image) in inputs)
        {
            var caseDirectory = Path.Combine(directory, item.Id);
            Directory.CreateDirectory(caseDirectory);
            var sourceName = "source" + image.Extension;
            await File.WriteAllBytesAsync(Path.Combine(caseDirectory, sourceName), image.Bytes);
            await File.WriteAllTextAsync(Path.Combine(caseDirectory, "source.json"), JsonSerializer.Serialize(new
                { kind = "image", file = sourceName, sha256 = image.Sha256, visual_review_required = true }, RecipeJson.Options));
            if (suite is not null)
                File.Copy(ImageEvaluationSuite.Resolve(manifestPath, item.Reference), Path.Combine(caseDirectory, "reference.json"));
            Console.WriteLine("Odczyt: " + item.Id);
            var extractor = new RecipeImageExtractor(http, model, prompt, schema, options, entry =>
                File.AppendAllTextAsync(Path.Combine(directory, "turns.jsonl"),
                    JsonSerializer.Serialize(new { case_id = item.Id, entry }) + Environment.NewLine));
            try
            {
                var result = await extractor.ExtractRecipeAsync(image);
                if (result.Document is not null)
                    await File.WriteAllTextAsync(Path.Combine(caseDirectory, "draft.json"), JsonSerializer.Serialize(result.Document, RecipeJson.Options));
                await File.WriteAllTextAsync(Path.Combine(caseDirectory, "validation.json"), JsonSerializer.Serialize(result.Validation, RecipeJson.Options));
                review.Add(new(item.Id, true, result.Validation.Status, result.ElapsedMs, null));
                Console.WriteLine($"{item.Id}: {result.Validation.Status}; {result.ElapsedMs / 1000d:F1} s. Odczyt i cytaty wymagają sprawdzenia z obrazem.");
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException)
            {
                await File.WriteAllTextAsync(Path.Combine(caseDirectory, "error.txt"), ex.ToString());
                review.Add(new(item.Id, false, null, null, ex.Message));
                Console.Error.WriteLine(item.Id + ": " + ex.Message + ". Kontynuuję pozostałe obrazy.");
            }
            await WriteReview();
        }
        var completed = review.Count(row => row.TechnicalCompleted);
        Console.WriteLine($"Ukończono technicznie: {completed}/{inputs.Length}. To nie ocena jakości. Uzupełnij review.json po porównaniu z obrazami.");
        Console.WriteLine("Nie zatwierdzono ani nie zapisano przepisu do bazy. Wyniki: " + directory);
        return review.All(row => row.TechnicalCompleted && row.ValidationStatus != "invalid_output") ? 0 : 1;

        async Task WriteReview()
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "review.json"), JsonSerializer.Serialize(review, RecipeJson.Options));
            // Puste pola jakości pozostają puste także przy poprawnym JSON i zerowym kodzie wyjścia.
            var lines = new List<string> { "case_id,technical_completed,validation_status,elapsed_ms,ocr_pass,interpretation_pass,critical_error,passed,notes" };
            lines.AddRange(review.Select(row => string.Join(',', row.CaseId, row.TechnicalCompleted ? "true" : "false",
                row.ValidationStatus ?? "", row.ElapsedMs?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "", "", "", "", "", "")));
            await File.WriteAllLinesAsync(Path.Combine(directory, "scorecard.csv"), lines);
        }
    }
}
