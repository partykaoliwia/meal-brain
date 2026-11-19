using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;

namespace MealBrain.ModelLab.Recipes;

public static class ExtractionConsole
{
    public static async Task<int> RunAsync(string model, string? suiteName = null,
        HttpClient? httpClient = null, string? runRoot = null)
    {
        if (suiteName is not null && !ExtractionCheckSuite.IsKnownName(suiteName))
            throw new InvalidDataException("Nieznany zestaw ekstrakcji.");
        var promptVersion = ExtractionCheckSuite.PromptVersion(suiteName);
        var variant = "s3-extraction-" + promptVersion;
        var prompt = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, $"extraction-prompt-{promptVersion}.txt"));
        var schemaText = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "recipe-draft-v1.schema.json"));
        var schema = JsonSerializer.Deserialize<JsonElement>(schemaText);
        var cases = DevelopmentCases.ReadInputs(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "test-cases.md")));
        ExtractionCheckSuite? suite = null;
        string? suiteHash = null;
        if (suiteName is not null)
        {
            var suitePath = Path.Combine(AppContext.BaseDirectory, suiteName + ".json");
            suite = ExtractionCheckSuite.Load(suitePath);
            suite.VerifyContext(prompt, schemaText);
            suiteHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(suitePath)));
        }
        var directory = Path.GetFullPath(Path.Combine(runRoot ?? "runs", $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-extract-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(directory);
        using var ownedHttp = httpClient is null
            ? new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(10) }
            : null;
        var http = httpClient ?? ownedHttp!;
        Console.WriteLine($"S3 — ekstrakcja JSON ({variant}), model {model}. Logi: {directory}");
        try
        {
            var version = await http.GetFromJsonAsync<JsonElement>("/api/version");
            var installed = await http.GetFromJsonAsync<JsonElement>("/api/tags");
            await File.WriteAllTextAsync(Path.Combine(directory, "metadata.json"), JsonSerializer.Serialize(new
            {
                mode = "extract", variant, schema_version = 1,
                validation_version = RecipeDraftValidator.Version,
                suite_id = suite?.Id, suite_inputs_sha256 = suiteHash,
                started_at_utc = DateTimeOffset.UtcNow, model, think = false,
                options = new { temperature = 0, num_ctx = 8192, num_predict = 4096 },
                system_prompt = prompt, schema,
                prompt_sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(prompt))),
                schema_sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(schemaText))),
                ollama_version = version, installed_models = installed, machine = Environment.MachineName,
                runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            Console.Error.WriteLine($"Nie można odczytać metadanych Ollamy: {ex.Message}. Sprawdź ollama list.");
            return 1;
        }

        var inputId = 0;
        string? activeCaseId = null;
        var pending = new Queue<ExtractionCheckCase>(suite?.Cases ?? []);
        var review = new List<object>();
        var completedCount = 0;
        var extractor = new RecipeExtractor(http, model, prompt, schema, entry =>
            File.AppendAllTextAsync(Path.Combine(directory, "turns.jsonl"),
                JsonSerializer.Serialize(new { input_id = inputId, case_id = activeCaseId, entry }) + Environment.NewLine));
        if (suite is null)
        {
            Console.WriteLine("Każde wejście to osobny import. Komendy: /case MB-001 (do MB-006), /paste, /exit.");
            Console.WriteLine("/paste: wklej opis i zakończ osobną linią /send. Uzupełnienia i zatwierdzanie nie są jeszcze obsługiwane w tym trybie.");
        }
        else Console.WriteLine($"Zestaw {suite.Id}: {suite.Cases.Length} niezależnych odczytów. Nie wpisuj wiadomości; ocena jakości pozostaje ręczna.");
        while (true)
        {
            string? input;
            activeCaseId = null;
            if (suite is not null)
            {
                if (!pending.TryDequeue(out var next)) break;
                activeCaseId = next.Id;
                input = next.Input;
                Console.WriteLine($"\n{next.Id}, odczyt {inputId + 1}/{suite.Cases.Length}");
            }
            else
            {
                Console.Write("\nTekst> ");
                input = Console.ReadLine();
            }
            if (input is null || input == "/exit") break;
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (suite is null && input.StartsWith("/case ", StringComparison.OrdinalIgnoreCase))
            {
                if (!cases.TryGetValue(input[6..].Trim(), out var source))
                { Console.WriteLine("Wybierz MB-001–MB-006."); continue; }
                activeCaseId = input[6..].Trim();
                input = source;
            }
            else if (suite is null && input == "/paste")
            {
                var lines = new List<string>();
                while (Console.ReadLine() is { } line && line != "/send") lines.Add(line);
                input = string.Join(Environment.NewLine, lines);
                if (string.IsNullOrWhiteSpace(input)) continue;
            }
            else if (suite is null && input.StartsWith('/')) { Console.WriteLine("Nieznana komenda."); continue; }
            inputId++;
            var prefix = Path.Combine(directory, $"extract-{inputId:000}");
            await File.WriteAllTextAsync(prefix + ".source.txt", input);
            Console.WriteLine("Odczytuję przepis. Czekam na pełny JSON...");
            string? validationStatus = null;
            string? failure = null;
            long? elapsedMs = null;
            var completed = false;
            try
            {
                var result = await extractor.ExtractRecipeAsync(input);
                completed = true;
                completedCount++;
                validationStatus = result.Validation.Status;
                elapsedMs = result.ElapsedMs;
                if (result.Document is not null)
                {
                    var draftJson = JsonSerializer.Serialize(result.Document, RecipeJson.Options);
                    await File.WriteAllTextAsync(prefix + ".draft.json", draftJson);
                    Console.WriteLine(draftJson);
                }
                await File.WriteAllTextAsync(prefix + ".validation.json",
                    JsonSerializer.Serialize(result.Validation, RecipeJson.Options));
                Console.WriteLine($"\nStan wyliczony przez C#: {result.Validation.Status}; czas: {result.ElapsedMs} ms.");
                foreach (var error in result.Validation.Errors) Console.WriteLine($"Błąd danych: {error}");
                foreach (var field in result.Validation.MissingFields) Console.WriteLine($"Do uzupełnienia: {field}");
                foreach (var issue in result.Validation.ReviewIssues) Console.WriteLine($"Do przeglądu: {issue}");
                Console.WriteLine("To wynik odczytu i kontroli danych. Nie zatwierdzono ani nie zapisano przepisu do bazy.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
            {
                failure = ex.Message;
                Console.Error.WriteLine($"Błąd odczytu: {ex.Message}. Szczegóły w turns.jsonl.");
            }
            if (suite is not null)
            {
                review.Add(new { input_id = inputId, case_id = activeCaseId, technical_completed = completed,
                    validation_status = validationStatus, elapsed_ms = elapsedMs, error = failure,
                    semantic_pass = (bool?)null, notes = (string?)null });
                await File.WriteAllTextAsync(Path.Combine(directory, "review.json"),
                    JsonSerializer.Serialize(review, RecipeJson.Options));
            }
        }
        if (suite is not null)
            Console.WriteLine($"Ukończone technicznie odczyty: {completedCount}/{suite.Cases.Length}. To nie wynik jakości; semantic_pass w review.json pozostaje pusty.");
        Console.WriteLine($"Koniec ekstrakcji. Zapis wyników: {directory}");
        return suite is null || completedCount == suite.Cases.Length ? 0 : 1;
    }
}
