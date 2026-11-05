using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using MealBrain.ModelLab.Recipes;

// Kontrola mechaniki na sztucznym obrazie i atrapach HTTP; nie jest oceną modeli ani OCR.
static class ImageExtractionChecks
{
    public static async Task RunAsync()
    {
        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("S1: " + message);
        }
        static void Reject(Action action, string message)
        {
            try { action(); } catch (InvalidDataException) { return; }
            throw new Exception("S1: " + message);
        }
        var root = Path.Combine(Path.GetTempPath(), "mealbrain-s1-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        // Rzeczywisty jednobarwny PNG 1x1, wyłącznie jako fixture transportu obrazu.
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jWZkAAAAASUVORK5CYII=");
        var imagePath = Path.Combine(root, "screen.png");
        await File.WriteAllBytesAsync(imagePath, bytes);
        var image = RecipeImage.Load(imagePath);
        Check(image.Extension == ".png" && image.Bytes.SequenceEqual(bytes), "zmieniono bajty obrazu.");
        var invalidImage = Path.Combine(root, "not-image.png");
        await File.WriteAllTextAsync(invalidImage, "to nie PNG");
        Reject(() => RecipeImage.Load(invalidImage), "zaakceptowano tekst z rozszerzeniem PNG.");

        var prompt = File.ReadAllText(ImageExtractionConsole.PromptPath);
        var schemaText = File.ReadAllText(ImageExtractionConsole.SchemaPath);
        var schema = JsonSerializer.Deserialize<JsonElement>(schemaText);
        var raw = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "recipe-draft-v1.example.json"));
        var referenceDocument = RecipeJson.Parse(raw);
        var expectedStatus = RecipeDraftValidator.Validate(referenceDocument).Status;
        var payloads = new List<string>();
        var logs = new List<string>();
        using var http = new HttpClient(new FakeHandler(async request =>
        {
            payloads.Add(await request.Content!.ReadAsStringAsync());
            return Reply(raw);
        })) { BaseAddress = new Uri("http://localhost:11434") };
        var extractor = new RecipeImageExtractor(http, "vision-model", prompt, schema, new(), entry =>
        { logs.Add(JsonSerializer.Serialize(entry)); return Task.CompletedTask; });
        var result = await extractor.ExtractRecipeAsync(image);
        await extractor.ExtractRecipeAsync(image);
        Check(result.Document is not null && result.Validation.Status == expectedStatus &&
            result.Validation.ReviewIssues.Length == 0, "użyto nazwy pliku/base64 jako tekstu źródłowego lub zmieniono reguły RecipeDraft.");
        foreach (var payloadText in payloads)
        {
            using var parsed = JsonDocument.Parse(payloadText);
            var payload = parsed.RootElement;
            var messages = payload.GetProperty("messages");
            Check(messages.GetArrayLength() == 2 && messages[0].GetProperty("role").GetString() == "system" &&
                messages[1].GetProperty("role").GetString() == "user", "historia poprzedniego importu trafiła do nowego odczytu.");
            Check(Convert.FromBase64String(messages[1].GetProperty("images")[0].GetString()!).SequenceEqual(bytes),
                "model nie otrzymał dokładnych bajtów obrazu.");
            Check(messages[1].GetProperty("content").GetString() == RecipeImageExtractor.UserInstruction &&
                !payloadText.Contains(imagePath) && !payload.TryGetProperty("think", out _), "wyciek ścieżki lub nieobsługiwane ustawienie think.");
            Check(payload.GetProperty("format").GetProperty("type").GetString() == "object" &&
                payload.GetProperty("options").GetProperty("num_ctx").GetInt32() == 16384 &&
                !payload.GetProperty("stream").GetBoolean(), "brak wspólnego schematu lub inne ustawienia.");
        }
        Check(logs.All(line => !line.Contains(Convert.ToBase64String(bytes)) && line.Contains(image.Sha256)),
            "log powiela base64 lub nie identyfikuje obrazu.");

        foreach (var answer in new[] { "to nie JSON", raw.Insert(raw.IndexOf('{') + 1, "\"approved\":true,") })
        {
            using var badHttp = new HttpClient(new FakeHandler(_ => Task.FromResult(Reply(answer))))
                { BaseAddress = http.BaseAddress };
            var bad = await new RecipeImageExtractor(badHttp, "vision-model", prompt, schema, new(), _ => Task.CompletedTask)
                .ExtractRecipeAsync(image);
            Check(bad.Validation.Status == "invalid_output", "zaakceptowano błędny dokument lub zgodę modelu.");
        }
        using (var truncatedHttp = new HttpClient(new FakeHandler(_ => Task.FromResult(Reply(raw, "length")))) { BaseAddress = http.BaseAddress })
        {
            try
            {
                await new RecipeImageExtractor(truncatedHttp, "vision-model", prompt, schema, new(), _ => Task.CompletedTask)
                    .ExtractRecipeAsync(image);
                throw new Exception("S1: przyjęto uciętą odpowiedź.");
            }
            catch (InvalidDataException) { }
        }

        var reference = new ImageReference(true, "SECRET_GOLD_TRANSCRIPTION", expectedStatus, referenceDocument,
            ["SECRET_GOLD_EXPECTATION"], "https://example.invalid/SECRET_SOURCE", null);
        var referencePath = Path.Combine(root, "reference.json");
        await File.WriteAllTextAsync(referencePath, JsonSerializer.Serialize(reference, RecipeJson.Options));
        var suite = new ImageEvaluationSuite("test-images-v1", new(),
            [new("S1-001", "screen.png", "reference.json"), new("S1-002", "screen.png", "reference-2.json"),
             new("S1-003", "screen.png", "reference-3.json")]);
        // Oddzielne pliki wejść i wzorców; nie używamy ich jako niezależnych danych jakościowych.
        for (var i = 2; i <= 3; i++)
        {
            File.Copy(imagePath, Path.Combine(root, $"screen-{i}.png"));
            File.Copy(referencePath, Path.Combine(root, $"reference-{i}.json"));
        }
        suite = suite with { Cases = [suite.Cases[0], new("S1-002", "screen-2.png", "reference-2.json"), new("S1-003", "screen-3.png", "reference-3.json")] };
        var manifestPath = Path.Combine(root, "suite.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(suite, RecipeJson.Options));
        suite = ImageEvaluationSuite.Load(manifestPath);
        Check(suite.CheckFiles(manifestPath).Length == 0, "poprawny zestaw nie przeszedł przygotowania.");
        Reject(() => suite.VerifyFrozen(manifestPath, prompt, schemaText), "uruchomiono niezamarznięty zestaw.");
        await File.WriteAllTextAsync(referencePath, JsonSerializer.Serialize(reference with { Ready = false }, RecipeJson.Options));
        Reject(() => suite.Freeze(manifestPath, prompt, schemaText), "zamrożono nieuzupełniony wzorzec.");
        await File.WriteAllTextAsync(referencePath, JsonSerializer.Serialize(reference, RecipeJson.Options));
        suite.Freeze(manifestPath, prompt, schemaText);
        suite.VerifyFrozen(manifestPath, prompt, schemaText);
        Reject(() => suite.Freeze(manifestPath, prompt, schemaText), "nadpisano protokół zamrożenia.");
        Reject(() => suite.VerifyFrozen(manifestPath, prompt + "zmiana", schemaText), "nie wykryto zmiany promptu.");
        Reject(() => suite.VerifyFrozen(manifestPath, prompt, schemaText + " "), "nie wykryto zmiany schematu.");
        await File.AppendAllTextAsync(referencePath, " ");
        Reject(() => suite.VerifyFrozen(manifestPath, prompt, schemaText), "nie wykryto zmiany wzorca.");
        await File.WriteAllTextAsync(referencePath, JsonSerializer.Serialize(reference, RecipeJson.Options));
        await File.WriteAllBytesAsync(imagePath, bytes.Concat(new byte[] { 0 }).ToArray());
        Reject(() => suite.VerifyFrozen(manifestPath, prompt, schemaText), "nie wykryto zmiany obrazu.");
        await File.WriteAllBytesAsync(imagePath, bytes);
        Reject(() => ImageEvaluationSuite.Resolve(manifestPath, "../outside.png"), "dopuszczono ścieżkę poza zestawem.");

        var chats = new List<string>();
        using var suiteHttp = new HttpClient(new FakeHandler(async request =>
        {
            switch (request.RequestUri!.AbsolutePath)
            {
                case "/api/version": return JsonReply("{\"version\":\"fake\"}");
                case "/api/tags": return JsonReply("{\"models\":[{\"name\":\"vision-model\",\"digest\":\"fake-digest\"}]}");
                case "/api/show": return JsonReply("{\"capabilities\":[\"completion\",\"vision\"]}");
            }
            chats.Add(await request.Content!.ReadAsStringAsync());
            if (chats.Count == 2) return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("kontrolowany błąd") };
            return Reply(chats.Count == 3 ? "błędny JSON" : raw);
        })) { BaseAddress = http.BaseAddress };
        var runRoot = Path.Combine(root, "runs");
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var captured = new StringWriter();
        int code;
        try
        {
            Console.SetOut(captured); Console.SetError(captured);
            code = await ImageExtractionConsole.RunAsync("vision-model", manifestPath, true, suiteHttp, runRoot);
        }
        finally { Console.SetOut(originalOut); Console.SetError(originalError); }
        Check(code == 1 && chats.Count == 3, "błąd HTTP zatrzymał pozostałe obrazy albo nie zmienił kodu wyjścia.");
        Check(chats.All(payload => !payload.Contains("SECRET_GOLD") && !payload.Contains("SECRET_SOURCE")),
            "wzorzec lub transkrypcja wyciekły do modelu.");
        var runDirectory = Directory.GetDirectories(runRoot).Single();
        using var rowsJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(runDirectory, "review.json")));
        var rows = rowsJson.RootElement.EnumerateArray().ToArray();
        Check(rows.Length == 3 && !rows[1].GetProperty("technicalCompleted").GetBoolean() &&
            rows[2].GetProperty("validationStatus").GetString() == "invalid_output", "pomylono awarię API z błędem danych.");
        Check(rows.All(row => new[] { "ocrPass", "interpretationPass", "criticalError", "passed" }
            .All(key => row.GetProperty(key).ValueKind == JsonValueKind.Null)), "runner sam nadał ocenę jakości.");
        Check(File.ReadAllBytes(Path.Combine(runDirectory, "S1-001", "source.png")).SequenceEqual(bytes) &&
            File.Exists(Path.Combine(runDirectory, "S1-001", "reference.json")) &&
            File.Exists(Path.Combine(runDirectory, "protocol.lock.json")), "nie zachowano źródeł i zamrożonego protokołu.");

        var chatCalled = false;
        using var textHttp = new HttpClient(new FakeHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/chat") chatCalled = true;
            return Task.FromResult(JsonReply(request.RequestUri.AbsolutePath == "/api/show"
                ? "{\"capabilities\":[\"completion\"]}" : "{}"));
        })) { BaseAddress = http.BaseAddress };
        try
        {
            Console.SetOut(captured); Console.SetError(captured);
            code = await ImageExtractionConsole.RunAsync("text-model", imagePath, false, textHttp, Path.Combine(root, "text-runs"));
        }
        finally { Console.SetOut(originalOut); Console.SetError(originalError); }
        Check(code == 1 && !chatCalled, "wysłano obraz do modelu bez vision.");
        Console.WriteLine("OK: S1 — bajty obrazu, wspólny RecipeDraft, izolacja importów, brak wycieku wzorców, blokada modelu tekstowego, zamrożenie warunków, logi i puste oceny jakości.");
    }

    private static HttpResponseMessage JsonReply(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Reply(string content, string reason = "stop") => JsonReply(
        JsonSerializer.Serialize(new { message = new { content }, done = true, done_reason = reason }));
}
