using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var suite = EvaluationSuite.Load(Path.Combine(AppContext.BaseDirectory, "finalists-v1.json"));
var prompt = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "system-prompt.txt"));
suite.VerifyPrompt(prompt);
Check(suite.Cases.Length == 12 && suite.ExecutionCount == 20 && suite.ResponseCount == 41, "Nieprawidłowy zakres próby.");
try
{
    suite.VerifyPrompt(prompt + "zmiana");
    throw new Exception("Nie wykryto zmiany instrukcji.");
}
catch (InvalidDataException) { }

var requests = new List<SuiteTurn>();
var checkpoints = 0;
var runs = await SuiteRunner.RunAsync(suite, prompt, turn =>
{
    requests.Add(turn);
    Check(turn.Messages[0] == new Message("system", prompt), "Brak oryginalnej instrukcji.");
    var item = suite.Cases.Single(c => c.Id == turn.CaseId);
    Check(turn.Messages[^1] == new Message("user", item.Messages[turn.Turn - 1]), "Zmieniono wypowiedź użytkowniczki.");
    Check(turn.Messages.Count == 2 * turn.Turn, "Nieprawidłowa historia.");
    Check(turn.Messages.Where(m => m.Role == "user").Select(m => m.Content)
        .SequenceEqual(item.Messages.Take(turn.Turn)), "Historia pochodzi z innego przypadku lub powtórzenia.");
    return Task.FromResult(new ChatReply("Odpowiedź kontrolna", 123));
}, _ => { checkpoints++; return Task.CompletedTask; });
Check(requests.Count == 41 && runs.Count == 20 && checkpoints == 20, "Nie wykonano pełnej próby.");
Check(runs.All(r => r.Status == "completed" && r.FirstResponseMs == 123), "Błędny status albo pomiar.");
Check(runs.Select(r => r.ConversationId).Distinct().Count() == 20, "Powtórzenia współdzielą identyfikator rozmowy.");
Check(requests.Where(r => r.Turn == 1).All(r => r.Messages.Count == 2), "Przeniesiono historię między rozmowami.");

var faultSuite = new EvaluationSuite("control", suite.PromptSha256,
    [new("FT-901", "Błąd w drugiej turze", 1, ["pierwsza", "druga", "nie wysyłaj"]),
     new("FT-902", "Niezależna rozmowa", 1, ["nowa"])]);
var attempted = new List<SuiteTurn>();
var faultRuns = await SuiteRunner.RunAsync(faultSuite, prompt, turn =>
{
    attempted.Add(turn);
    if (turn.CaseId == "FT-901" && turn.Turn == 2) throw new HttpRequestException("Kontrolny błąd HTTP");
    return Task.FromResult(new ChatReply("OK", 10));
}, _ => Task.CompletedTask);
Check(attempted.Count == 3 && faultRuns[0].Status == "technical_error" &&
    faultRuns[0].CompletedTurns == 1 && faultRuns[1].Status == "completed", "Błędna obsługa przerwanej rozmowy.");
Check(attempted[^1].Messages.Count == 2, "Następny przypadek odziedziczył nieudaną rozmowę.");

var payloads = new List<string>();
var logs = new List<string>();
using var http = new HttpClient(new FakeHandler(async request =>
{
    payloads.Add(await request.Content!.ReadAsStringAsync());
    return new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent("""{"message":{"content":"Wynik"},"done":true,"done_reason":"stop"}""", Encoding.UTF8, "application/json")
    };
})) { BaseAddress = new Uri("http://localhost:11434") };
var serializer = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
var client = new BatchOllamaClient(http, "test-model", false, new { temperature = 0, num_ctx = 8192, num_predict = 2048 },
    serializer, entry => { logs.Add(JsonSerializer.Serialize(entry)); return Task.CompletedTask; });
await client.SendAsync(new("FT-001", 1, 1, "control", [new("system", prompt), new("user", "Tylko wejście")]));
using (var payload = JsonDocument.Parse(payloads.Single()))
{
    var root = payload.RootElement;
    Check(root.GetProperty("messages").GetArrayLength() == 2 && !root.GetProperty("think").GetBoolean() &&
        !root.GetProperty("stream").GetBoolean(), "Błędna serializacja API.");
    Check(root.EnumerateObject().All(p => p.Name is "model" or "messages" or "stream" or "think" or "options" or "keep_alive"),
        "Do żądania trafiły dane spoza kontraktu API.");
    Check(!payloads[0].Contains("PromptSha256") && !payloads[0].Contains("Repetitions"), "Do API trafił obiekt zestawu.");
}

using var truncatedHttp = new HttpClient(new FakeHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
{
    Content = new StringContent("""{"message":{"content":"Urwana odpowiedź"},"done":true,"done_reason":"length"}""")
}))) { BaseAddress = new Uri("http://localhost:11434") };
var truncated = new BatchOllamaClient(truncatedHttp, "test-model", false, new { temperature = 0 }, serializer,
    entry => { logs.Add(JsonSerializer.Serialize(entry)); return Task.CompletedTask; });
try
{
    await truncated.SendAsync(new("FT-001", 1, 1, "control", [new("user", "wejście")]));
    throw new Exception("Ucięta odpowiedź została zaakceptowana.");
}
catch (InvalidDataException) { }
Check(logs.Last().Contains("\"kind\":\"error\""), "Nie zapisano błędu ucięcia odpowiedzi.");

// Uruchamiamy prawdziwy tryb batch z fikcyjnym HTTP, bez serwera i bez modelu.
var temporaryDirectory = Path.Combine(Path.GetTempPath(), "mealbrain-batch-check-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporaryDirectory);
var code = await BatchMode.RunAsync(faultSuite, prompt, client, temporaryDirectory);
Check(code == 0, "Tryb batch nie zakończył poprawnej próby.");
var csv = File.ReadAllLines(Path.Combine(temporaryDirectory, "scorecard.csv"));
Check(csv.Length == 3 && csv.All(line => line.Split(',').Length == 9), "Nieprawidłowa karta ocen CSV.");
Check(csv.Skip(1).All(line => line.Split(',').Skip(3).All(string.IsNullOrEmpty)), "Program sam nadał ocenę jakości.");

await ExtractionChecks.RunAsync();
Console.WriteLine("OK: zakres 12/20/41, zamrożenie promptu, izolacja historii i powtórzeń, uzupełnienia, błędy, payload API, ucięcia i pusta karta ocen. Żaden model nie został wywołany.");

sealed class FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
}
