using System.Net;
using System.Text;
using System.Text.Json;
using MealBrain.ModelLab.Recipes;

// Sprawdzenia propozycji zmian bez prawdziwego modelu: odpowiedź Ollamy jest podstawiona.
static class DraftUpdateChecks
{
    public static async Task RunAsync()
    {
        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        static DraftSession Load(int id)
        {
            var prefix = Path.Combine(AppContext.BaseDirectory, "extraction-check-v2", $"extract-{id:000}");
            return new DraftSession(RecipeJson.Parse(File.ReadAllText(prefix + ".draft.json")),
                File.ReadAllText(prefix + ".source.txt"));
        }
        static DraftUpdateProposal Proposal(string json) => JsonSerializer.Deserialize<DraftUpdateProposal>(json, RecipeJson.Options)!;

        // Ogórki: „2 porcje, 100 g jogurtu” jako dwie akcje.
        var cucumbers = Load(2);
        var outcome = DraftUpdateApplier.Apply(cucumbers, Proposal("""
            {"intent":"update","clarification":null,"actions":[
              {"type":"setServings","ingredientId":null,"conflictId":null,"servings":2,"amount":null,"minutes":null,"reviewIssue":null},
              {"type":"setIngredientAmount","ingredientId":"i2","conflictId":null,"servings":null,
               "amount":{"kind":"exact","value":100,"min":null,"max":null,"unit":"g","text":null},"minutes":null,"reviewIssue":null}]}
            """), "2 porcje, 100 g jogurtu");
        Check(outcome.Results.All(r => r.Applied) && cucumbers.State == DraftState.ReadyForReview, "Ogórki: propozycja nie uzupełniła braków.");

        // Model twierdzi, że użytkowniczka zatwierdza, ale szkic ma braki: aplikacja odmawia.
        var sauce = Load(6);
        var early = DraftUpdateApplier.Apply(sauce, Proposal("""{"intent":"approve","actions":[],"clarification":null}"""), "Zatwierdzam");
        Check(!early.Results.Single().Applied && sauce.ApprovedRevision is null, "Sos: zatwierdzono szkic z konfliktem.");

        // Model próbuje ominąć konflikt zwykłą zmianą ilości: odrzucone. Nieistniejący składnik: odrzucony.
        var bypass = DraftUpdateApplier.Apply(sauce, Proposal("""
            {"intent":"update","clarification":null,"actions":[
              {"type":"setIngredientAmount","ingredientId":"i2","conflictId":null,"servings":null,
               "amount":{"kind":"exact","value":30,"min":null,"max":null,"unit":"ml","text":null},"minutes":null,"reviewIssue":null},
              {"type":"setIngredientAmount","ingredientId":"i99","conflictId":null,"servings":null,
               "amount":{"kind":"exact","value":1,"min":null,"max":null,"unit":"g","text":null},"minutes":null,"reviewIssue":null}]}
            """), "30 ml");
        Check(bypass.Results.All(r => !r.Applied) && sauce.Revision == 1, "Sos: przyjęto błędną propozycję.");
        var resolved = DraftUpdateApplier.Apply(sauce, Proposal("""
            {"intent":"update","clarification":null,"actions":[
              {"type":"resolveConflict","ingredientId":null,"conflictId":"c1","servings":null,
               "amount":{"kind":"exact","value":30,"min":null,"max":null,"unit":"ml","text":null},"minutes":null,"reviewIssue":null}]}
            """), "Poprawne jest 30 ml.");
        Check(resolved.Results.Single().Applied && sauce.State == DraftState.ReadyForReview, "Sos: konflikt nie został rozstrzygnięty.");

        // Kanapki: potwierdzenie uwagi nr 1, potem zgoda.
        var sandwiches = Load(3);
        DraftUpdateApplier.Apply(sandwiches, Proposal("""
            {"intent":"update","clarification":null,"actions":[
              {"type":"acknowledgeReview","ingredientId":null,"conflictId":null,"servings":null,"amount":null,"minutes":null,"reviewIssue":1}]}
            """), "To literówka, w porządku.");
        var approved = DraftUpdateApplier.Apply(sandwiches, Proposal("""{"intent":"approve","actions":[],"clarification":null}"""), "Zatwierdzam.");
        Check(approved.Results.Single().Applied && sandwiches.State == DraftState.Approved, "Kanapki: nie zatwierdzono po przeglądzie.");

        // Ryż: „jeszcze nie zatwierdzam” nie nadaje zgody.
        var rice = Load(1);
        DraftUpdateApplier.Apply(rice, Proposal("""{"intent":"decline","actions":[],"clarification":null}"""), "Jeszcze nie zatwierdzam.");
        Check(rice.State == DraftState.ReadyForReview, "Ryż: odmowa zmieniła stan.");

        // Interpreter: kontekst i żądanie do Ollamy, z podstawioną odpowiedzią.
        string? payload = null;
        var content = JsonSerializer.Serialize("""{"intent":"decline","actions":[],"clarification":null}""");
        using var http = new HttpClient(new FakeHandler(async request =>
        {
            payload = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{"message":{"content":{{content}}},"done":true,"done_reason":"stop"}""", Encoding.UTF8, "application/json")
            };
        })) { BaseAddress = new Uri("http://localhost:11434") };
        var schema = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "draft-update-v1.schema.json")));
        var prompt = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "draft-update-prompt-v1.txt"));
        var logs = new List<string>();
        var interpreter = new DraftUpdateInterpreter(http, "test-model", prompt, schema,
            entry => { logs.Add(JsonSerializer.Serialize(entry)); return Task.CompletedTask; });
        var interpreted = await interpreter.InterpretAsync(Load(2), "Jeszcze nie zatwierdzam.");
        Check(interpreted.Proposal?.Intent == "decline" && interpreted.Error is null, "Interpreter: nie odczytano propozycji.");
        using (var sent = JsonDocument.Parse(payload!))
        {
            var user = sent.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!;
            Check(user.Contains("\"id\": \"i2\"") && user.Contains("servings") && user.EndsWith("Jeszcze nie zatwierdzam."),
                "Interpreter: kontekst nie zawiera szkicu, braków i wypowiedzi.");
            Check(sent.RootElement.GetProperty("format").GetProperty("title").GetString() == "DraftUpdateProposal v1",
                "Interpreter: brak schematu odpowiedzi.");
        }
        Check(logs.Single().Contains("\"kind\":\"interpret\""), "Interpreter: brak logu.");

        Console.WriteLine("OK: propozycje zmian — uzupełnienie, blokada przedwczesnej zgody, ominięcia konfliktu i złych id, przegląd cytatu, odmowa i żądanie do Ollamy.");
    }
}

static class DraftConsoleChecks
{
    public static async Task RunAsync()
    {
        // Konsola z podstawionym modelem: ogórki, uzupełnienie i zgoda.
        var replies = new Queue<string>([
            """{"intent":"update","clarification":null,"actions":[{"type":"setServings","ingredientId":null,"conflictId":null,"servings":2,"amount":null,"minutes":null,"reviewIssue":null},{"type":"setIngredientAmount","ingredientId":"i2","conflictId":null,"servings":null,"amount":{"kind":"exact","value":100,"min":null,"max":null,"unit":"g","text":null},"minutes":null,"reviewIssue":null}]}""",
            """{"intent":"approve","actions":[],"clarification":null}"""]);
        using var http = new HttpClient(new FakeHandler(_ => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent($$"""{"message":{"content":{{System.Text.Json.JsonSerializer.Serialize(replies.Dequeue())}}},"done":true,"done_reason":"stop"}""")
        }))) { BaseAddress = new Uri("http://localhost:11434") };
        var root = Path.Combine(Path.GetTempPath(), "mealbrain-draft-check-" + Guid.NewGuid().ToString("N"));
        var output = new StringWriter();
        var original = Console.Out;
        Console.SetOut(output);
        try
        {
            await MealBrain.ModelLab.Recipes.DraftConsole.RunAsync("test-model",
                new StringReader("/case ogorki\n2 porcje, 100 g jogurtu\nZatwierdzam\n/history\n/exit\n"), http, root);
        }
        finally { Console.SetOut(original); }
        var text = output.ToString();
        if (!text.Contains("Zatwierdzono rewizję 3") || replies.Count != 0)
            throw new Exception("Konsola szkicu: nie przeszła ścieżki uzupełnienie → zgoda.\n" + text);
        var log = Directory.GetFiles(root, "turns.jsonl", SearchOption.AllDirectories).Single();
        if (File.ReadAllLines(log).Length != 5) throw new Exception("Konsola szkicu: niepełny log.");
        Console.WriteLine("OK: konsola rozmowy na szkicu — wczytanie przypadku, uzupełnienie, zgoda, historia i log.");
    }
}
