using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MealBrain.ModelLab.Recipes;

public sealed record ImageEvaluationCase(string Id, string Image, string Reference);
public sealed record ImageReference(bool Ready, string? Transcription, string? ExpectedValidationStatus,
    RecipeExtractionDocument? ExpectedDraft, string[] RequiredChecks, string? SourceUrl, string? Notes);
public sealed record ImageCaseHashes(string Id, string ImageSha256, string ReferenceSha256);
public sealed record ImageEvaluationLock(string SuiteId, DateTimeOffset FrozenAtUtc, string ManifestSha256,
    string PromptSha256, string SchemaSha256, string ValidationVersion, ImageCaseHashes[] Cases);

public sealed record ImageEvaluationSuite(string Id, ImageExtractionOptions Options, ImageEvaluationCase[] Cases)
{
    public static ImageEvaluationSuite Load(string path)
    {
        var suite = JsonSerializer.Deserialize<ImageEvaluationSuite>(File.ReadAllText(path), RecipeJson.Options)
            ?? throw new InvalidDataException("Pusty zestaw obrazów.");
        if (!SafeId(suite.Id) || suite.Options is null || suite.Cases is null || suite.Cases.Length is < 1 or > 100)
            throw new InvalidDataException("Nieprawidłowy nagłówek zestawu obrazów.");
        suite.Options.Validate();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var images = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in suite.Cases)
        {
            if (item is null || !SafeId(item.Id) || !ids.Add(item.Id) ||
                !images.Add(Resolve(path, item.Image)) || !references.Add(Resolve(path, item.Reference)))
                throw new InvalidDataException("Powtórzony identyfikator, obraz lub wzorzec w zestawie.");
        }
        return suite;
    }

    public string[] CheckFiles(string manifestPath)
    {
        var issues = new List<string>();
        foreach (var item in Cases)
        {
            try { RecipeImage.Load(Resolve(manifestPath, item.Image)); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            { issues.Add(item.Id + ": " + ex.Message); }
            try
            {
                var reference = JsonSerializer.Deserialize<ImageReference>(
                    File.ReadAllText(Resolve(manifestPath, item.Reference)), RecipeJson.Options)
                    ?? throw new InvalidDataException("Pusty wzorzec.");
                if (!reference.Ready || string.IsNullOrWhiteSpace(reference.Transcription) || reference.ExpectedDraft is null ||
                    reference.RequiredChecks is null || reference.RequiredChecks.Length == 0 ||
                    reference.RequiredChecks.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidDataException("Uzupełnij wzorzec i ustaw ready=true dopiero po ręcznym przeglądzie.");
                // Parse wykrywa też brakujące pola, powtórzone klucze i pola spoza wspólnego kontraktu.
                var document = RecipeJson.Parse(JsonSerializer.Serialize(reference.ExpectedDraft, RecipeJson.Options));
                var validation = RecipeDraftValidator.Validate(document);
                if (validation.Status == "invalid_output" || validation.Status != reference.ExpectedValidationStatus)
                    throw new InvalidDataException("Wzorcowy RecipeDraft jest niepoprawny lub ma inny status walidacji niż oczekiwany.");
            }
            catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
            { issues.Add(item.Id + " — wzorzec: " + ex.Message); }
        }
        return issues.ToArray();
    }

    public string Freeze(string manifestPath, string prompt, string schema)
    {
        var lockPath = LockPath(manifestPath);
        if (File.Exists(lockPath)) throw new InvalidDataException("Protokół już istnieje. Dla zmienionych warunków utwórz nową wersję zestawu.");
        var frozen = Snapshot(manifestPath, prompt, schema);
        using var stream = new FileStream(lockPath, FileMode.CreateNew, FileAccess.Write);
        JsonSerializer.Serialize(stream, frozen, RecipeJson.Options);
        return lockPath;
    }

    public ImageEvaluationLock VerifyFrozen(string manifestPath, string prompt, string schema)
    {
        var path = LockPath(manifestPath);
        if (!File.Exists(path)) throw new InvalidDataException("Najpierw zamroź zestaw komendą --freeze-image-suite.");
        var frozen = JsonSerializer.Deserialize<ImageEvaluationLock>(File.ReadAllText(path), RecipeJson.Options)
            ?? throw new InvalidDataException("Pusty protokół.");
        var current = Snapshot(manifestPath, prompt, schema);
        if (frozen.SuiteId != current.SuiteId || frozen.ManifestSha256 != current.ManifestSha256 ||
            frozen.PromptSha256 != current.PromptSha256 || frozen.SchemaSha256 != current.SchemaSha256 ||
            frozen.ValidationVersion != current.ValidationVersion || frozen.Cases is null ||
            !frozen.Cases.SequenceEqual(current.Cases))
            throw new InvalidDataException("Obrazy, wzorce, instrukcja, schemat lub ustawienia zmieniły się po zamrożeniu. Utwórz nową wersję zestawu.");
        return frozen;
    }

    private ImageEvaluationLock Snapshot(string manifestPath, string prompt, string schema)
    {
        var issues = CheckFiles(manifestPath);
        if (issues.Length > 0) throw new InvalidDataException(string.Join(Environment.NewLine, issues));
        return new(Id, DateTimeOffset.UtcNow, HashFile(manifestPath), HashText(prompt), HashText(schema),
            RecipeDraftValidator.Version, Cases.Select(item => new ImageCaseHashes(item.Id,
                HashFile(Resolve(manifestPath, item.Image)), HashFile(Resolve(manifestPath, item.Reference)))).ToArray());
    }

    public static string Resolve(string manifestPath, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("Ścieżka obrazu i wzorca musi być względna wobec zestawu.");
        var root = Path.GetDirectoryName(Path.GetFullPath(manifestPath))! + Path.DirectorySeparatorChar;
        var resolved = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Ścieżka wychodzi poza katalog zestawu.");
        return resolved;
    }

    public static string LockPath(string manifestPath) => Path.ChangeExtension(Path.GetFullPath(manifestPath), ".lock.json");
    public static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    public static string HashText(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    private static bool SafeId(string? id) => id is not null && Regex.IsMatch(id, "^[A-Za-z0-9][A-Za-z0-9_-]{0,79}$");
}
