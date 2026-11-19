using Microsoft.Extensions.AI;
using OllamaSharp;

namespace MealBrain.ModelLab.Recipes;

// Rozmowa na szkicu z tool callingiem (lab 04): UseFunctionInvocation obsługuje cykl narzędzi.
public static class DraftToolConsole
{
    private static readonly Dictionary<string, int> Cases = new() { ["ryz"] = 1, ["ogorki"] = 2, ["kanapki"] = 3, ["sos"] = 6 };

    public const string PcssEndpoint = "https://llm.hpc.psnc.pl/v1";

    // Użycie: --tools [model] [--provider ollama|pcss] [--endpoint url]. PCSS: klucz w zmiennej PCSS_API_KEY.
    public static async Task<int> RunAsync(string[] args)
    {
        string? model = null, provider = "ollama", endpoint = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--provider" && i + 1 < args.Length) provider = args[++i];
            else if (args[i] == "--endpoint" && i + 1 < args.Length) endpoint = args[++i];
            else model ??= args[i];
        }
        if (provider is not ("ollama" or "pcss")) { Console.WriteLine("Nieznany provider. Dostępne: ollama, pcss."); return 1; }
        var prompt = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "draft-tools-prompt-v1.txt"));
        // Domyślny timeout HttpClient (100 s) jest za krótki przy pierwszym ładowaniu modelu.
        using var http = new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(10) };
        IChatClient inner;
        if (provider == "pcss")
        {
            var key = Environment.GetEnvironmentVariable("PCSS_API_KEY");
            if (string.IsNullOrWhiteSpace(key))
            {
                Console.WriteLine("Brak zmiennej środowiskowej PCSS_API_KEY w tym procesie. Ustaw ją w tym samym oknie PowerShell, z którego uruchamiasz program.");
                return 1;
            }
            model ??= "DeepSeek-V3.1";
            var client = new OpenAI.OpenAIClient(new System.ClientModel.ApiKeyCredential(key),
                new OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpoint ?? PcssEndpoint), NetworkTimeout = TimeSpan.FromMinutes(5) });
            inner = client.GetChatClient(model).AsIChatClient();
        }
        else
        {
            model ??= "gemma4:12b";
            inner = new OllamaApiClient(http, model);
        }
        Console.WriteLine($"Provider: {provider}, model: {model}");
        IChatClient chat = new ChatClientBuilder(inner).UseFunctionInvocation().Build();
        var options = new ChatOptions { Temperature = 0 };

        DraftSession? session = null;
        DraftTools? tools = null;
        var history = new List<ChatMessage>();
        var lastUser = "";
        var outDir = Path.GetFullPath(Path.Combine("runs", $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-tools"));

        Console.WriteLine($"Rozmowa z narzędziami, model {model}. Komendy: /case ryz|ogorki|kanapki|sos, /show, /history, /exit.");
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine()?.Trim();
            if (line is null || line == "/exit") break;
            if (line.Length == 0) continue;
            if (line.StartsWith("/case"))
            {
                var name = line[5..].Trim();
                if (!Cases.TryGetValue(name, out var file)) { Console.WriteLine("Dostępne: " + string.Join(", ", Cases.Keys)); continue; }
                var prefix = Path.Combine(AppContext.BaseDirectory, "drafts", $"extract-{file:000}");
                var source = await File.ReadAllTextAsync(prefix + ".source.txt");
                session = new DraftSession(RecipeJson.Parse(await File.ReadAllTextAsync(prefix + ".draft.json")), source);
                tools = new DraftTools(session, ConfirmSave, () => lastUser);
                options.Tools = tools.Create();
                history = [new(ChatRole.System, prompt)];
                Console.WriteLine($"Źródło:\n{source.Trim()}\n");
                DraftConsole.Print(session);
                continue;
            }
            if (session is null || tools is null) { Console.WriteLine("Najpierw: /case ogorki"); continue; }
            if (line == "/show") { DraftConsole.Print(session); continue; }
            if (line == "/history")
            {
                foreach (var h in session.History) Console.WriteLine($"  r{h.Revision} {h.Operation} {h.Target}: {h.OldValue ?? "-"} → {h.NewValue ?? "-"}");
                continue;
            }

            lastUser = line;
            history.Add(new(ChatRole.User, line));
            var traceStart = tools.Trace.Count;
            try
            {
                var response = await chat.GetResponseAsync(history, options);
                history.AddMessages(response);
                foreach (var t in tools.Trace.Skip(traceStart)) Console.WriteLine("  [tool] " + t.Replace("\n", "\n         "));
                Console.WriteLine("Asystent: " + response.Text);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                Console.WriteLine($"Błąd modelu: {ex.Message}. Szkic bez zmian.");
                history.RemoveAt(history.Count - 1);
            }
            if (tools.SavedJson is not null)
            {
                Directory.CreateDirectory(outDir);
                var path = Path.Combine(outDir, "saved-recipe.json");
                await File.WriteAllTextAsync(path, tools.SavedJson);
                Console.WriteLine($"Zapisano plik: {path}");
            }
        }
        return 0;
    }

    private static bool ConfirmSave(int revision)
    {
        Console.Write($"  Czy zapisać rewizję {revision}? [t/N] ");
        return string.Equals(Console.ReadLine()?.Trim(), "t", StringComparison.OrdinalIgnoreCase);
    }
}
