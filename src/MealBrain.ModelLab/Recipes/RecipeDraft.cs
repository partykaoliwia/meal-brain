using System.Text.Json;
using System.Text.Json.Serialization;

namespace MealBrain.ModelLab.Recipes;

public sealed record RecipeExtractionDocument
{
    public required int SchemaVersion { get; init; }
    public required string InputKind { get; init; }
    public required string? RejectionReason { get; init; }
    public required RecipeDraft? Recipe { get; init; }
}

public sealed record RecipeDraft
{
    public required string? Title { get; init; }
    public required int? Servings { get; init; }
    public required RecipeTimes Times { get; init; }
    public required IngredientDraft[] Ingredients { get; init; }
    public required StepDraft[] Steps { get; init; }
    public required ConflictDraft[] Conflicts { get; init; }
}

public sealed record RecipeTimes
{
    public required MinuteRange? Total { get; init; }
    public required MinuteRange? Preparation { get; init; }
    public required MinuteRange? Cooking { get; init; }
    public required MinuteRange? Resting { get; init; }
}

public sealed record MinuteRange
{
    public required decimal MinMinutes { get; init; }
    public required decimal MaxMinutes { get; init; }
}

public sealed record IngredientDraft
{
    public required string Id { get; init; }
    public required string? Name { get; init; }
    public required AmountDraft Amount { get; init; }
    public required bool Optional { get; init; }
    public required string[] Qualifiers { get; init; }
    public required string SourceText { get; init; }
}

public sealed record AmountDraft
{
    public required string Kind { get; init; }
    public required decimal? Value { get; init; }
    public required decimal? Min { get; init; }
    public required decimal? Max { get; init; }
    public required string? Unit { get; init; }
    public required string? Text { get; init; }
}

public sealed record StepDraft
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public required string[] IngredientIds { get; init; }
    public required string SourceText { get; init; }
}

public sealed record ConflictDraft
{
    public required string Id { get; init; }
    public required ConflictTarget Target { get; init; }
    public required ConflictCandidate[] Candidates { get; init; }
}

public sealed record ConflictTarget
{
    public required string Type { get; init; }
    public required string? Id { get; init; }
}

public sealed record ConflictCandidate
{
    public required string ValueText { get; init; }
    public required string SourceText { get; init; }
}

public static class RecipeJson
{
    // Jawne null pozostają w plikach. Pola dodatkowe, np. approved, są odrzucane.
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        WriteIndented = true
    };

    public static RecipeExtractionDocument Parse(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        RejectDuplicateProperties(parsed.RootElement);
        return JsonSerializer.Deserialize<RecipeExtractionDocument>(json, Options)
            ?? throw new JsonException("Odpowiedź nie zawiera dokumentu RecipeDraft.");
    }

    private static void RejectDuplicateProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException($"Powtórzone pole: {property.Name}.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) RejectDuplicateProperties(item);
    }
}
