using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MealBrain.ModelLab.Recipes;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

if (args.Length > 0 && ImageCommands.Handles(args[0]))
    return await ImageCommands.RunAsync(args);

if (args.Length > 0 && args[0] == "--extract")
{
    var validExtractArgs = args.Length <= 2 ||
        (args.Length == 4 && args[2] == "--suite" && ExtractionCheckSuite.IsKnownName(args[3]));
    if (!validExtractArgs)
    {
        Console.Error.WriteLine("Użycie: dotnet run --project src/MealBrain.ModelLab -- --extract [model] [--suite s3-extraction-check-v1|s3-extraction-check-v2]");
        return 1;
    }
    return await ExtractionConsole.RunAsync(args.ElementAtOrDefault(1) ?? "gemma4:12b", args.ElementAtOrDefault(3));
}

var prompt = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "system-prompt.txt"));
var suitePath = Path.Combine(AppContext.BaseDirectory, "finalists-v1.json");
if (args is ["--check-suite"])
{
    var checkedSuite = EvaluationSuite.Load(suitePath);
    checkedSuite.VerifyPrompt(prompt);
    Console.WriteLine($"OK: {checkedSuite.Id}, {checkedSuite.Cases.Length} przypadków, {checkedSuite.ExecutionCount} rozmów, {checkedSuite.ResponseCount} odpowiedzi. Instrukcja v2 zgodna. Nie wywołano Ollamy.");
    return 0;
}

if ((args.Length > 3 && !(args.Length == 5 && args[3] == "--suite" && args[4] == "finalists-v1")) ||
    (args.Length > 1 && args[1] is not ("on" or "off" or "default")))
{
    Console.Error.WriteLine("Użycie: dotnet run --project src/MealBrain.ModelLab -- [model] [off|on|default] [temperature] [--suite finalists-v1]");
    return 1;
}

var model = args.ElementAtOrDefault(0) ?? "qwen3.5:4b";
var thinkingMode = args.ElementAtOrDefault(1) ?? "off";
if (!double.TryParse(args.ElementAtOrDefault(2) ?? "0", NumberStyles.Float,
        CultureInfo.InvariantCulture, out var temperature) || !double.IsFinite(temperature) || temperature < 0 || temperature > 2)
{
    Console.Error.WriteLine("Temperature: liczba od 0 do 2, z kropką, np. 0.5.");
    return 1;
}

var suite = args.Length == 5 ? EvaluationSuite.Load(suitePath) : null;
suite?.VerifyPrompt(prompt);
var casesDocument = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "test-cases.md"));
// Wyłącznie wejścia, bez odpowiedzi wzorcowych.
var cases = DevelopmentCases.ReadInputs(casesDocument);

var runDirectory = Path.GetFullPath(Path.Combine("runs", $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"));
Directory.CreateDirectory(runDirectory);
var serializer = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
var options = new { temperature, num_ctx = 8192, num_predict = 2048 };
bool? think = thinkingMode == "default" ? null : thinkingMode == "on";
using var http = new HttpClient
{
    BaseAddress = new Uri("http://localhost:11434"),
    Timeout = TimeSpan.FromMinutes(10)
};

async Task LogAsync(object entry) => await File.AppendAllTextAsync(
    Path.Combine(runDirectory, "turns.jsonl"), JsonSerializer.Serialize(entry, serializer) + Environment.NewLine);

Console.WriteLine($"Model: {model}; thinking: {thinkingMode}; temperature: {temperature.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"Logi: {runDirectory}");

try
{
    var version = await http.GetFromJsonAsync<JsonElement>("/api/version");
    var installed = await http.GetFromJsonAsync<JsonElement>("/api/tags");
    await File.WriteAllTextAsync(Path.Combine(runDirectory, "metadata.json"),
        JsonSerializer.Serialize(new
        {
            started_at_utc = DateTimeOffset.UtcNow, model, think, thinking_mode = thinkingMode,
            options, ollama_version = version, installed_models = installed,
            system_prompt = prompt,
            prompt_sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(prompt))),
            test_cases_sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                suite is null ? casesDocument : await File.ReadAllTextAsync(suitePath)))),
            suite_id = suite?.Id,
            mode = suite is null ? "interactive" : "batch",
            machine = Environment.MachineName,
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
        }, new JsonSerializerOptions(serializer) { WriteIndented = true }));
}
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
{
    Console.Error.WriteLine($"Nie można odczytać metadanych Ollamy: {ex.Message}");
    Console.Error.WriteLine("Uruchom Ollamę i sprawdź: ollama list. Następnie uruchom program ponownie.");
    return 1;
}

if (suite is not null)
    return await BatchMode.RunAsync(suite, prompt,
        new BatchOllamaClient(http, model, think, options, serializer, LogAsync), runDirectory);

var history = new List<Message> { new("system", prompt) };
string? caseId = null;
var conversationId = Guid.NewGuid().ToString("N");
Console.WriteLine("Komendy: /warmup, /case MB-001 (do MB-006), /new, /paste, /exit.");
Console.WriteLine("Zwykły tekst wysyła kolejną wiadomość. /paste: wklej wiele linii i zakończ osobną linią /send.");

while (true)
{
    Console.Write("\nTy> ");
    var input = Console.ReadLine();
    if (input is null || input == "/exit") break;
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input == "/new")
    {
        history = [new("system", prompt)];
        caseId = null;
        conversationId = Guid.NewGuid().ToString("N");
        Console.WriteLine("Nowa rozmowa. Historia poprzedniej pozostaje w logu.");
        continue;
    }

    var warmup = input == "/warmup";
    if (input.StartsWith("/case ", StringComparison.OrdinalIgnoreCase))
    {
        var selected = input[6..].Trim().ToUpperInvariant();
        if (!cases.TryGetValue(selected, out var caseInput))
        {
            Console.WriteLine("Wybierz MB-001, MB-002, MB-003, MB-004, MB-005 albo MB-006.");
            continue;
        }
        caseId = selected;
        conversationId = Guid.NewGuid().ToString("N");
        history = [new("system", prompt)];
        input = caseInput;
        Console.WriteLine($"Nowa rozmowa: {caseId}. Wysyłam wyłącznie wejście:\n{input}");
    }
    else if (input == "/paste")
    {
        var lines = new List<string>();
        while (Console.ReadLine() is { } line && line != "/send") lines.Add(line);
        input = string.Join(Environment.NewLine, lines);
        if (string.IsNullOrWhiteSpace(input)) continue;
    }
    else if (input.StartsWith('/') && !warmup)
    {
        Console.WriteLine("Nieznana komenda.");
        continue;
    }

    List<Message> messages = warmup
        ? [new("user", "Odpowiedz krótko: OK.")]
        : [.. history, new("user", input)];
    var request = new { model, messages, stream = false, think, options, keep_alive = "10m" };
    var startedAt = DateTimeOffset.UtcNow;
    var watch = Stopwatch.StartNew();
    string? raw = null;
    try
    {
        Console.WriteLine("Czekam na pełną odpowiedź...");
        using var response = await http.PostAsJsonAsync("/api/chat", request, serializer);
        raw = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(raw);
        var root = json.RootElement;
        var answer = root.GetProperty("message").GetProperty("content").GetString() ?? "";
        watch.Stop();
        if (string.IsNullOrWhiteSpace(answer))
            throw new InvalidDataException("Model nie zwrócił końcowej odpowiedzi. Sprawdź limit tokenów i tryb thinking w logu.");
        if (!warmup)
        {
            history.Add(new("user", input));
            history.Add(new("assistant", answer));
        }
        await LogAsync(new { kind = warmup ? "warmup" : "chat", case_id = warmup ? null : caseId,
            conversation_id = warmup ? null : conversationId,
            started_at_utc = startedAt, elapsed_ms = watch.ElapsedMilliseconds, request, response = root });
        Console.WriteLine($"\nAgent> {answer}");
        var loadMs = root.TryGetProperty("load_duration", out var load) ? load.GetDouble() / 1_000_000 : 0;
        Console.WriteLine($"\nCzas: {watch.ElapsedMilliseconds} ms; ładowanie według Ollamy: {loadMs:F0} ms.");
        if (root.TryGetProperty("done_reason", out var reason) && reason.GetString() == "length")
            Console.WriteLine("UWAGA: osiągnięto limit odpowiedzi. Zapisz to jako błąd/ograniczenie przebiegu.");
        if (warmup) Console.WriteLine("Rozgrzewka zakończona; nie została dodana do historii przypadków.");
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
    {
        watch.Stop();
        await LogAsync(new { kind = "error", case_id = warmup ? null : caseId,
            conversation_id = warmup ? null : conversationId,
            started_at_utc = startedAt, elapsed_ms = watch.ElapsedMilliseconds, request, raw_response = raw, error = ex.Message });
        Console.Error.WriteLine($"Błąd: {ex.Message}\nSzczegóły w logu. Nie dodano tej wiadomości do historii.");
        Console.Error.WriteLine("Sprawdź ollama list, nazwę modelu i obsługę think. /case uruchamia przypadek ponownie.");
    }
}

Console.WriteLine($"Koniec. Zapis rozmów: {runDirectory}");
return 0;

public record Message(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);
