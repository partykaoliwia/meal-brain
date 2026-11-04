using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public sealed record EvaluationSuite(string Id, string PromptSha256, EvaluationCase[] Cases)
{
    public static EvaluationSuite Load(string path)
    {
        var suite = JsonSerializer.Deserialize<EvaluationSuite>(File.ReadAllText(path),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException("Pusty zestaw testowy.");
        if (string.IsNullOrWhiteSpace(suite.Id) || suite.PromptSha256 is null ||
            !Regex.IsMatch(suite.PromptSha256, "^[A-Fa-f0-9]{64}$") ||
            suite.Cases is null || suite.Cases.Length == 0)
            throw new InvalidDataException("Nieprawidłowy nagłówek zestawu.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in suite.Cases)
        {
            if (item is null || item.Id is null || !Regex.IsMatch(item.Id, "^FT-[0-9]{3}$") ||
                !ids.Add(item.Id) || string.IsNullOrWhiteSpace(item.Title) ||
                item.Repetitions is < 1 or > 3 || item.Messages is null ||
                item.Messages.Length == 0 || item.Messages.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException("Nieprawidłowy lub powtórzony przypadek testowy.");
        }
        return suite;
    }

    public void VerifyPrompt(string prompt)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(prompt)));
        if (!string.Equals(hash, PromptSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Instrukcja różni się od zamrożonej v2. Przywróć system-prompt-v2.txt z docs/stage-02/prompts przed porównaniem finalistów.");
    }

    public int ExecutionCount => Cases.Sum(c => c.Repetitions);
    public int ResponseCount => Cases.Sum(c => c.Repetitions * c.Messages.Length);
}

public sealed record EvaluationCase(string Id, string Title, int Repetitions, string[] Messages);
