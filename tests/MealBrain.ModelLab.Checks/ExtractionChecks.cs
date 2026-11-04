using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MealBrain.ModelLab.Recipes;

static class ExtractionChecks
{
    public static async Task RunAsync()
    {
        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("Ekstrakcja: " + message);
        }
        var raw = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "recipe-draft-v1.example.json"));
        var example = RecipeJson.Parse(raw);
        RecipeExtractionDocument Change(Action<JsonObject> change)
        {
            var node = JsonNode.Parse(raw)!.AsObject();
            change(node);
            return RecipeJson.Parse(node.ToJsonString());
        }
        static JsonObject Recipe(JsonObject node) => node["recipe"]!.AsObject();
        static JsonObject Oil(JsonObject node) => Recipe(node)["ingredients"]![1]!.AsObject();
        static void Complete(JsonObject node)
        {
            var amount = Oil(node)["amount"]!.AsObject();
            amount["kind"] = "exact"; amount["value"] = 1; amount["unit"] = "łyżka";
        }
        var validation = RecipeDraftValidator.Validate(example);
        Check(validation.Status == "needs_clarification" && validation.Errors.Length == 0 &&
            validation.MissingFields.Length == 1 && validation.MissingFields[0].Contains("ingredients[i2].amount"),
            "brak ilości oliwy musi być jedynym brakiem; sól do smaku nie wymaga liczby.");
        Check(RecipeDraftValidator.Validate(Change(Complete)).Status == "ready_for_review", "kompletny szkic nie jest gotowy do przeglądu.");
        Check(RecipeDraftValidator.Validate(Change(n => Oil(n)["optional"] = true)).Status == "ready_for_review", "opcjonalny składnik bez ilości blokuje przegląd.");
        Check(RecipeDraftValidator.Validate(Change(n => { Complete(n); Oil(n)["amount"]!["min"] = 2; })).Status == "invalid_output", "kind exact akceptuje jednocześnie min.");
        Check(RecipeDraftValidator.Validate(Change(n => { Complete(n); Oil(n)["amount"]!["value"] = -1; })).Status == "invalid_output", "akceptuje ujemną ilość.");
        Check(RecipeDraftValidator.Validate(Change(n => { Complete(n); Recipe(n)["servings"] = 0; })).Status == "invalid_output", "akceptuje zero porcji.");
        Check(RecipeDraftValidator.Validate(Change(n => { Complete(n); Recipe(n)["times"]!["total"]!["maxMinutes"] = 10; })).Status == "invalid_output", "akceptuje odwrócony zakres czasu.");
        Check(RecipeDraftValidator.Validate(Change(n => Recipe(n)["steps"]![0]!["ingredientIds"]![0] = "i999")).Status == "invalid_output", "akceptuje nieistniejący składnik w kroku.");
        Check(RecipeDraftValidator.Validate(Change(n => Oil(n)["id"] = "i1")).Status == "invalid_output", "akceptuje powtórzone id.");
        Check(RecipeDraftValidator.Validate(Change(n => { Oil(n)["amount"]!["kind"] = "text"; Oil(n)["amount"]!["text"] = "nie podano"; })).Status != "ready_for_review", "tekst nie podano obchodzi kontrolę braków.");

        var conflict = JsonNode.Parse("""
            {"id":"c1","target":{"type":"ingredientAmount","id":"i2"},"candidates":[
              {"valueText":"1 łyżka","sourceText":"1 łyżka oliwy"},
              {"valueText":"2 łyżki","sourceText":"2 łyżki oliwy"}]}
            """)!;
        var conflicting = Change(n => Recipe(n)["conflicts"]!.AsArray().Add(conflict.DeepClone()));
        var conflictValidation = RecipeDraftValidator.Validate(conflicting);
        Check(conflictValidation.Errors.Length == 0 && conflictValidation.MissingFields.Length == 1 &&
            conflictValidation.MissingFields[0].StartsWith("conflicts[c1]"), "konflikt nie zastępuje zwykłego pytania o tę samą ilość.");
        Check(RecipeDraftValidator.Validate(Change(n => { Complete(n); Recipe(n)["conflicts"]!.AsArray().Add(conflict.DeepClone()); })).Status == "invalid_output",
            "rozstrzygnięta liczba nie może być efektywna przy nierozwiązanym konflikcie.");
        Check(RecipeDraftValidator.Validate(Change(n => { var bad = conflict.DeepClone(); bad["target"]!["id"] = "i999"; Recipe(n)["conflicts"]!.AsArray().Add(bad); })).Status == "invalid_output",
            "akceptuje nieistniejący cel konfliktu.");
        var times = Change(n => { Complete(n); Recipe(n)["times"]!["total"] = null;
            Recipe(n)["times"]!["cooking"] = JsonNode.Parse("""{"minMinutes":25,"maxMinutes":35}"""); });
        Check(RecipeDraftValidator.Validate(times).MissingFields.Single().StartsWith("times.total"), "wylicza czas całkowity z czasu gotowania.");
        Check(RecipeDraftValidator.Validate(RecipeJson.Parse("""{"schemaVersion":1,"inputKind":"not_recipe","rejectionReason":"Ogłoszenie bez przepisu","recipe":null}""")).Status == "not_recipe",
            "nie obsługuje materiału bez przepisu.");
        foreach (var invalidJson in new[]
        {
            raw.Insert(raw.IndexOf('{') + 1, "\"approved\":true,"),
            raw.Insert(raw.IndexOf('{') + 1, "\"schemaVersion\":1,"),
            """{"schemaVersion":1,"inputKind":"recipe","rejectionReason":null}"""
        })
        {
            try { RecipeJson.Parse(invalidJson); throw new Exception("Akceptuje nadmiarowe, powtórzone lub brakujące pole."); }
            catch (JsonException) { }
        }
        var mismatch = RecipeDraftValidator.Validate(example, "Ten tekst nie zawiera wskazanych cytatów.");
        Check(mismatch.ReviewIssues.Length > 0 && mismatch.Status == "needs_clarification", "nie wykrywa cytatów spoza źródła.");

        var schema = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "recipe-draft-v1.schema.json")));
        var prompt = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "extraction-prompt-v2.txt"));
        var payloads = new List<string>();
        var logs = new List<string>();
        var source = string.Join("\n", example.Recipe!.Ingredients.Select(i => i.SourceText)
            .Concat(example.Recipe.Steps.Select(s => s.SourceText)));
        using var http = new HttpClient(new FakeHandler(async request =>
        {
            payloads.Add(await request.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent(JsonSerializer.Serialize(new { message = new { content = raw }, done = true, done_reason = "stop" }), Encoding.UTF8, "application/json") };
        })) { BaseAddress = new Uri("http://localhost:11434") };
        var extractor = new RecipeExtractor(http, "test-model", prompt, schema,
            entry => { logs.Add(JsonSerializer.Serialize(entry)); return Task.CompletedTask; });
        var result = await extractor.ExtractRecipeAsync(source);
        await extractor.ExtractRecipeAsync(source + "\nNowy niezależny import.");
        Check(result.Document is not null && result.Validation.Status == "needs_clarification" &&
            result.Validation.ReviewIssues.Length == 0, "błędny wynik odczytu i kontroli.");
        foreach (var payload in payloads)
        {
            using var parsed = JsonDocument.Parse(payload);
            var request = parsed.RootElement;
            Check(request.GetProperty("messages").GetArrayLength() == 2, "przenosi historię między importami.");
            Check(request.GetProperty("format").GetProperty("title").GetString() == "RecipeDraft v1", "nie przekazuje JSON Schema.");
            Check(!request.GetProperty("think").GetBoolean() && !request.GetProperty("stream").GetBoolean(), "błędne parametry API.");
        }

        using var badHttp = new HttpClient(new FakeHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { message = new { content = raw.Insert(raw.IndexOf('{') + 1, "\"approved\":true,") }, done = true, done_reason = "stop" }))
        }))) { BaseAddress = new Uri("http://localhost:11434") };
        var rejected = await new RecipeExtractor(badHttp, "test-model", prompt, schema, _ => Task.CompletedTask).ExtractRecipeAsync(source);
        Check(rejected.Document is null && rejected.Validation.Status == "invalid_output", "approved w odpowiedzi modelu nie jest odrzucane.");

        using var truncatedHttp = new HttpClient(new FakeHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { message = new { content = raw }, done = true, done_reason = "length" }))
        }))) { BaseAddress = new Uri("http://localhost:11434") };
        try
        {
            await new RecipeExtractor(truncatedHttp, "test-model", prompt, schema, _ => Task.CompletedTask).ExtractRecipeAsync(source);
            throw new Exception("Akceptuje uciętą odpowiedź ekstrakcji.");
        }
        catch (InvalidDataException) { }
        var development = DevelopmentCases.ReadInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "test-cases.md")));
        Check(development.Count == 6 && development.Values.All(v => v.StartsWith("Zaimportuj") && !v.Contains("### Oczekiwane")), "wczytuje odpowiedzi wzorcowe jako wejście.");

        // Rzeczywiste, niepoprawiane odpowiedzi z pierwszej próby ekstrakcji.
        var captured = new List<(RecipeExtractionDocument Document, DraftValidation Validation)>();
        for (var id = 1; id <= 3; id++)
        {
            var prefix = Path.Combine(AppContext.BaseDirectory, "extraction-v1", $"extract-{id:000}");
            var document = RecipeJson.Parse(File.ReadAllText(prefix + ".draft.json"));
            captured.Add((document, RecipeDraftValidator.Validate(document, File.ReadAllText(prefix + ".source.txt"))));
        }
        Check(captured[0].Validation is { Status: "needs_clarification", Errors.Length: 0, MissingFields.Length: 3, ReviewIssues.Length: 0 },
            "rzeczywisty makaron musi zachować dokładnie trzy braki.");
        Check(captured[1].Validation.Status == "invalid_output" && captured[1].Validation.Errors.Length == 1 &&
            captured[1].Document.Recipe!.Ingredients.Single(i => i.Id == "i2").Amount.Kind == "range",
            "rzeczywisty konflikt 150/200 g nie może być zaakceptowany ani naprawiany po cichu.");
        Check(captured[2].Validation is { Status: "needs_clarification", Errors.Length: 0, MissingFields.Length: 1, ReviewIssues.Length: 1 } &&
            captured[2].Validation.ReviewIssues[0].Contains(".amount.text:"),
            "plastry powinny być obsługiwane; Trochę nadal wymaga przeglądu.");
        foreach (var unit in new[] { "plaster", "plastry", "plastrów", "kromka", "kromki", "kromek",
            "łyżki", "łyżek", "łyżeczki", "łyżeczek", "szklanki", "szklanek", "ząbki", "ząbków", "sztuki", "sztuk" })
            Check(RecipeDraftValidator.Validate(Change(n => { Complete(n); Oil(n)["amount"]!["unit"] = unit; })).Status == "ready_for_review",
                "nie obsługuje odmiany jednostki: " + unit);
        Check(RecipeDraftValidator.Validate(Change(n => { Complete(n); Oil(n)["amount"]!["unit"] = "garść"; })).ReviewIssues.Length == 1,
            "dodanie plastrów nie może automatycznie akceptować dowolnej jednostki.");

        for (var id = 1; id <= 2; id++)
        {
            var prefix = Path.Combine(AppContext.BaseDirectory, "extraction-v2", $"extract-{id:000}");
            var document = RecipeJson.Parse(File.ReadAllText(prefix + ".draft.json"));
            var checkedDraft = RecipeDraftValidator.Validate(document, File.ReadAllText(prefix + ".source.txt"));
            Check(checkedDraft is { Status: "needs_clarification", Errors.Length: 0, MissingFields.Length: 1 } &&
                checkedDraft.ReviewIssues.Length == id - 1, "nowy odczyt ma niewłaściwe blokady po uzupełnieniu odmian jednostek.");
            if (id == 1)
                Check(document.Recipe!.Ingredients.Single(i => i.Id == "i2").Amount.Kind == "unknown" &&
                    document.Recipe.Conflicts.Length == 1, "model nie zachował nierozstrzygniętego konfliktu.");
            else
                Check(document.Recipe!.Times.Cooking is { MinMinutes: 2, MaxMinutes: 2 } &&
                    document.Recipe.Ingredients.Single(i => i.Id == "i1").Amount is { Value: 2, Unit: "kromki" } &&
                    checkedDraft.ReviewIssues.Single().Contains(".amount.text:"),
                    "retest nie zachował czasu tostowania, oryginalnej jednostki lub przeglądu Trochę.");
        }

        var extractionSuite = ExtractionCheckSuite.Load(Path.Combine(AppContext.BaseDirectory, "s3-extraction-check-v1.json"));
        var schemaText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "recipe-draft-v1.schema.json"));
        extractionSuite.VerifyContext(prompt, schemaText);
        foreach (var altered in new[] { (prompt + "zmiana", schemaText), (prompt, schemaText + " ") })
        {
            try { extractionSuite.VerifyContext(altered.Item1, altered.Item2); throw new Exception("Nie wykryto zmiany warunków ekstrakcji."); }
            catch (InvalidDataException) { }
        }
        try { (extractionSuite with { ValidationVersion = "inna" }).VerifyContext(prompt, schemaText); throw new Exception("Nie wykryto zmiany walidatora."); }
        catch (InvalidDataException) { }
        var requestsBeforeSuite = payloads.Count;
        foreach (var extractionCase in extractionSuite.Cases)
            await extractor.ExtractRecipeAsync(extractionCase.Input);
        Check(payloads.Count == requestsBeforeSuite + 6, "zestaw nie ma sześciu niezależnych odczytów.");
        for (var id = 0; id < extractionSuite.Cases.Length; id++)
        {
            using var payload = JsonDocument.Parse(payloads[requestsBeforeSuite + id]);
            var messages = payload.RootElement.GetProperty("messages");
            Check(messages.GetArrayLength() == 2 && messages[1].GetProperty("content").GetString() == extractionSuite.Cases[id].Input,
                "źródło zestawu zmieniono lub połączono z historią innego importu.");
        }

        var regressionSuite = ExtractionCheckSuite.Load(Path.Combine(AppContext.BaseDirectory, "s3-extraction-check-v2.json"));
        var promptV3 = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "extraction-prompt-v3.txt"));
        regressionSuite.VerifyContext(promptV3, schemaText);
        Check(regressionSuite.Cases.SequenceEqual(extractionSuite.Cases), "regresja musi podawać dokładnie te same sześć wejść.");
        try { regressionSuite.VerifyContext(prompt, schemaText); throw new Exception("Regresja akceptuje starą instrukcję."); }
        catch (InvalidDataException) { }
        for (var index = 1; index <= 6; index++)
        {
            var prefix = Path.Combine(AppContext.BaseDirectory, "extraction-check-v1", $"extract-{index:000}");
            var document = RecipeJson.Parse(File.ReadAllText(prefix + ".draft.json"));
            var validationAtReview = RecipeDraftValidator.Validate(document, File.ReadAllText(prefix + ".source.txt"));
            if (index == 3)
                Check(validationAtReview is { Status: "needs_clarification", ReviewIssues.Length: 1 } &&
                    validationAtReview.ReviewIssues[0].Contains("sourceText"), "nie wykrywa zmienionego cytatu w rzeczywistych kanapkach.");
            if (index == 5)
                Check(document.Recipe!.Times.Total is { MinMinutes: 0, MaxMinutes: 5 } &&
                    validationAtReview.Status == "ready_for_review",
                    "sprawdzenie ograniczeń walidatora: poprawny kształt czasu nie dowodzi zgodności z tekstem.");
        }

        foreach (var automaticSuite in new[] { extractionSuite, regressionSuite })
        {
            var automaticPayloads = new List<string>();
            using var suiteHttp = new HttpClient(new FakeHandler(async request =>
            {
                if (request.RequestUri!.AbsolutePath == "/api/version")
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"version\":\"fake\"}") };
                if (request.RequestUri.AbsolutePath == "/api/tags")
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"models\":[]}") };
                automaticPayloads.Add(await request.Content!.ReadAsStringAsync());
                if (automaticPayloads.Count == 2)
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("kontrolowany błąd API") };
                var content = automaticPayloads.Count == 3 ? raw.Insert(raw.IndexOf('{') + 1, "\"approved\":true,") : raw;
                return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(JsonSerializer.Serialize(new { message = new { content }, done = true, done_reason = "stop" })) };
            })) { BaseAddress = new Uri("http://localhost:11434") };
            var runRoot = Path.Combine(Path.GetTempPath(), "mealbrain-extraction-check-" + Guid.NewGuid().ToString("N"));
            var originalOutput = Console.Out;
            var originalError = Console.Error;
            using var capturedConsole = new StringWriter();
            int suiteExitCode;
            try
            {
                Console.SetOut(capturedConsole);
                Console.SetError(capturedConsole);
                suiteExitCode = await ExtractionConsole.RunAsync("test-model", automaticSuite.Id, suiteHttp, runRoot);
            }
            finally { Console.SetOut(originalOutput); Console.SetError(originalError); }
            Check(suiteExitCode == 1 && automaticPayloads.Count == 6, "błąd API ma być odnotowany bez pomijania pozostałych wejść.");
            var runDirectory = Directory.GetDirectories(runRoot).Single();
            using var automaticReview = JsonDocument.Parse(File.ReadAllText(Path.Combine(runDirectory, "review.json")));
            var rows = automaticReview.RootElement.EnumerateArray().ToArray();
            Check(rows.Length == 6 && rows[1].GetProperty("technical_completed").ValueKind == JsonValueKind.False &&
                rows[2].GetProperty("validation_status").GetString() == "invalid_output" &&
                rows.All(row => row.GetProperty("semantic_pass").ValueKind == JsonValueKind.Null),
                "karta ocen myli wykonanie, walidację i ocenę jakości.");
            using var automaticMetadata = JsonDocument.Parse(File.ReadAllText(Path.Combine(runDirectory, "metadata.json")));
            Check(automaticMetadata.RootElement.GetProperty("suite_id").GetString() == automaticSuite.Id &&
                automaticMetadata.RootElement.GetProperty("validation_version").GetString() == RecipeDraftValidator.Version &&
                automaticMetadata.RootElement.GetProperty("prompt_sha256").GetString() == automaticSuite.PromptSha256 &&
                automaticMetadata.RootElement.GetProperty("variant").GetString() ==
                    (automaticSuite.Id == ExtractionCheckSuite.Name ? "s3-extraction-v2" : "s3-extraction-v3"),
                "brak właściwej wersji zestawu, promptu lub walidatora w metadanych.");
            for (var index = 0; index < 6; index++)
            {
                using var payload = JsonDocument.Parse(automaticPayloads[index]);
                Check(rows[index].GetProperty("case_id").GetString() == automaticSuite.Cases[index].Id &&
                    File.ReadAllText(Path.Combine(runDirectory, $"extract-{index + 1:000}.source.txt")) == automaticSuite.Cases[index].Input &&
                    payload.RootElement.GetProperty("messages").GetArrayLength() == 2 &&
                    payload.RootElement.GetProperty("messages")[1].GetProperty("content").GetString() == automaticSuite.Cases[index].Input,
                    "automatyczny tryb pomylił źródła, identyfikatory lub historię.");
            }
        }
        Console.WriteLine("OK: RecipeDraft — nieznane ilości, do smaku, opcjonalność, konflikty, identyfikatory, czasy, odrzucenia, źródła, JSON Schema w API i izolacja importów.");
        Console.WriteLine("OK: regresja na trzech rzeczywistych odczytach — braki makaronu, blokada konfliktu, plastry i przegląd ilości opisowej.");
        Console.WriteLine("OK: dwa odczyty retestu, odmiany jednostek oraz zamrożenie instrukcji, schematu i walidatora nowego zestawu sześciu ekstrakcji.");
        Console.WriteLine("OK: automatyczna ekstrakcja sześciu wejść, błąd HTTP bez zatrzymania zestawu, błędny dokument i puste oceny jakości.");
        Console.WriteLine("OK: stary zestaw zachowuje prompt v2; regresja wybiera prompt v3 i te same źródła; zmieniony cytat pozostaje oznaczony.");
    }
}
