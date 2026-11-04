using System.Text.RegularExpressions;

public static class DevelopmentCases
{
    public static Dictionary<string, string> ReadInputs(string document)
    {
        var cases = Regex.Matches(document,
                @"^## (MB-00[1-6]) — [^\r\n]+\r?\n(?<body>.*?)(?=^## |\z)",
                RegexOptions.Multiline | RegexOptions.Singleline)
            .ToDictionary(m => m.Groups[1].Value, m => Regex.Match(m.Groups["body"].Value,
                @"^```text\r?\n(?<input>.*?)^```", RegexOptions.Multiline | RegexOptions.Singleline)
                .Groups["input"].Value.Trim(), StringComparer.OrdinalIgnoreCase);
        if (cases.Count != 6 || cases.Values.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Nie udało się odczytać sześciu wejść z test-cases.md.");
        return cases;
    }
}
