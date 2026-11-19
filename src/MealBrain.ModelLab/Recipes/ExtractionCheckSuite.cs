using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MealBrain.ModelLab.Recipes;

public sealed record ExtractionCheckCase(string Id, string Input);

public sealed record ExtractionCheckSuite(string Id, string PromptSha256, string SchemaSha256,
    string ValidationVersion, ExtractionCheckCase[] Cases)
{
    public const string Name = "s3-extraction-check-v1";
    public const string RegressionName = "s3-extraction-check-v2";

    public static bool IsKnownName(string name) => name is Name or RegressionName;
    public static string PromptVersion(string? suiteName) => suiteName == Name ? "v2" : "v3";

    public static ExtractionCheckSuite Load(string path)
    {
        var suite = JsonSerializer.Deserialize<ExtractionCheckSuite>(File.ReadAllText(path), RecipeJson.Options)
            ?? throw new InvalidDataException("Pusty zestaw ekstrakcji.");
        if (!IsKnownName(suite.Id) || suite.Cases is null || suite.Cases.Length != 6 ||
            string.IsNullOrWhiteSpace(suite.ValidationVersion) ||
            suite.PromptSha256 is null || !Regex.IsMatch(suite.PromptSha256, "^[A-Fa-f0-9]{64}$") ||
            suite.SchemaSha256 is null || !Regex.IsMatch(suite.SchemaSha256, "^[A-Fa-f0-9]{64}$"))
            throw new InvalidDataException("Nieprawidłowy nagłówek zestawu ekstrakcji.");
        for (var index = 0; index < suite.Cases.Length; index++)
            if (suite.Cases[index] is not { } item || item.Id != $"EX-{index + 1:000}" || string.IsNullOrWhiteSpace(item.Input))
                throw new InvalidDataException("Nieprawidłowa kolejność lub wejście zestawu ekstrakcji.");
        return suite;
    }

    public void VerifyContext(string prompt, string schemaText)
    {
        static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        if (!string.Equals(Hash(prompt), PromptSha256, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Hash(schemaText), SchemaSha256, StringComparison.OrdinalIgnoreCase) ||
            ValidationVersion != RecipeDraftValidator.Version)
            throw new InvalidDataException("Instrukcja, schemat lub walidator różnią się od warunków zestawu ekstrakcji. Przygotuj nową wersję zestawu przed próbą.");
    }
}
