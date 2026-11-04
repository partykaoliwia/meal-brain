using System.Text.Json;

public static class BatchMode
{
    public static async Task<int> RunAsync(EvaluationSuite suite, string prompt,
        BatchOllamaClient client, string runDirectory)
    {
        Console.WriteLine($"Zestaw {suite.Id}: {suite.Cases.Length} przypadków, {suite.ExecutionCount} rozmów, {suite.ResponseCount} odpowiedzi + rozgrzewka.");
        Console.WriteLine("Wiadomości zostaną podane automatycznie. Ocena jakości pozostaje ręczna.");
        async Task Checkpoint(IReadOnlyList<CaseExecution> results)
        {
            await File.WriteAllTextAsync(Path.Combine(runDirectory, "suite-status.json"),
                JsonSerializer.Serialize(new { suite_id = suite.Id, results }, new JsonSerializerOptions { WriteIndented = true }));
            var rows = new List<string> { "case_id,repetition,technical_status,data_ok,questions_ok,context_ok,critical_error,passed,notes" };
            foreach (var item in suite.Cases)
                for (var repetition = 1; repetition <= item.Repetitions; repetition++)
                {
                    var status = results.FirstOrDefault(r => r.CaseId == item.Id && r.Repetition == repetition)?.Status ?? "pending";
                    rows.Add($"{item.Id},{repetition},{status},,,,,,");
                }
            await File.WriteAllLinesAsync(Path.Combine(runDirectory, "scorecard.csv"), rows);
        }

        await Checkpoint([]);
        try
        {
            Console.WriteLine("Rozgrzewka...");
            await client.SendAsync(new("warmup", 0, 0, Guid.NewGuid().ToString("N"),
                [new("user", "Odpowiedz krótko: OK.")]));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
        {
            Console.Error.WriteLine($"Rozgrzewka nie powiodła się: {ex.Message}. Testów nie uruchomiono.");
            return 1;
        }

        var results = await SuiteRunner.RunAsync(suite, prompt, client.SendAsync, Checkpoint, Console.WriteLine);
        var errors = results.Count(r => r.Status != "completed");
        Console.WriteLine($"Koniec zestawu. Ukończone technicznie rozmowy: {results.Count - errors}/{results.Count}.");
        Console.WriteLine("To nie jest wynik jakości. Pola oceny w scorecard.csv są celowo puste.");
        Console.WriteLine($"Zapis rozmów: {runDirectory}");
        return errors == 0 ? 0 : 2;
    }
}
