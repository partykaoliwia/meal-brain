using System.Text.RegularExpressions;

namespace MealBrain.ModelLab.Recipes;

public sealed record DraftValidation(string Status, string[] Errors, string[] MissingFields, string[] ReviewIssues);

public static class RecipeDraftValidator
{
    public const string Version = "recipe-draft-validation-v3";

    private static readonly HashSet<string> Units = new(StringComparer.OrdinalIgnoreCase)
        { "g", "kg", "ml", "l", "szt.", "szt", "sztuka", "sztuki", "sztuk",
          "łyżka", "łyżki", "łyżek", "łyżeczka", "łyżeczki", "łyżeczek",
          "szklanka", "szklanki", "szklanek", "ząbek", "ząbki", "ząbków", "kromka", "kromki", "kromek",
          "plaster", "plastry", "plastrów" };

    public static DraftValidation Validate(RecipeExtractionDocument document, string? originalText = null)
    {
        var errors = new List<string>();
        var missing = new List<string>();
        var review = new List<string>();
        DraftValidation Finish(string? status = null) => new(
            errors.Count > 0 ? "invalid_output" : status ??
                (missing.Count > 0 || review.Count > 0 ? "needs_clarification" : "ready_for_review"),
            errors.Distinct().ToArray(), missing.Distinct().ToArray(), review.Distinct().ToArray());
        void Evidence(string? text, string path)
        {
            if (string.IsNullOrWhiteSpace(text)) errors.Add($"{path}: pusty fragment źródła.");
            else if (originalText is not null && !originalText.Contains(text, StringComparison.Ordinal))
                review.Add($"{path}: cytat nie występuje dosłownie w wejściu; wymaga przeglądu.");
        }
        void Duration(MinuteRange? value, string path)
        {
            if (value is not null && (value.MinMinutes < 0 || value.MaxMinutes < value.MinMinutes))
                errors.Add($"{path}: nieprawidłowe granice czasu.");
        }

        if (document.SchemaVersion != 1) errors.Add("schemaVersion: wymagana wersja 1.");
        if (document.InputKind == "not_recipe")
        {
            if (document.Recipe is not null || string.IsNullOrWhiteSpace(document.RejectionReason))
                errors.Add("not_recipe wymaga recipe=null i niepustego rejectionReason.");
            return Finish("not_recipe");
        }
        if (document.InputKind != "recipe") errors.Add("inputKind: nieznany rodzaj wejścia.");
        if (document.RejectionReason is not null) errors.Add("recipe wymaga rejectionReason=null.");
        var recipe = document.Recipe;
        if (recipe is null) { errors.Add("Brak obiektu recipe."); return Finish(); }
        if (recipe.Times is null || recipe.Ingredients is null || recipe.Steps is null || recipe.Conflicts is null)
        { errors.Add("times, ingredients, steps i conflicts nie mogą być null."); return Finish(); }

        var ingredients = new Dictionary<string, IngredientDraft>(StringComparer.Ordinal);
        foreach (var ingredient in recipe.Ingredients)
        {
            if (ingredient is null) { errors.Add("ingredients zawiera null."); continue; }
            if (ingredient.Id is null || !Regex.IsMatch(ingredient.Id, "^i[1-9][0-9]*$") ||
                !ingredients.TryAdd(ingredient.Id, ingredient)) errors.Add("Nieprawidłowy lub powtórzony identyfikator składnika.");
        }
        var unresolved = new HashSet<string>(StringComparer.Ordinal);
        var conflictIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var conflict in recipe.Conflicts)
        {
            if (conflict is null) { errors.Add("conflicts zawiera null."); continue; }
            if (conflict.Id is null || !Regex.IsMatch(conflict.Id, "^c[1-9][0-9]*$") || !conflictIds.Add(conflict.Id))
                errors.Add("Nieprawidłowy lub powtórzony identyfikator konfliktu.");
            var path = $"conflicts[{conflict.Id}]";
            if (conflict.Candidates is null || conflict.Candidates.Length < 2)
                errors.Add($"{path}: wymagane co najmniej dwa warianty.");
            else
            {
                foreach (var candidate in conflict.Candidates)
                {
                    if (candidate is null) { errors.Add($"{path}: wariant null."); continue; }
                    if (string.IsNullOrWhiteSpace(candidate.ValueText)) errors.Add($"{path}: pusty wariant.");
                    Evidence(candidate.SourceText, path + ".sourceText");
                }
                if (conflict.Candidates.Where(c => c is not null && !string.IsNullOrWhiteSpace(c.ValueText))
                    .Select(c => c.ValueText.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2)
                    errors.Add($"{path}: warianty muszą się różnić.");
            }
            var target = conflict.Target;
            if (target is null) { errors.Add($"{path}: brak celu."); continue; }
            string? targetPath = null;
            if (target.Type == "ingredientAmount")
            {
                if (target.Id is null || !ingredients.TryGetValue(target.Id, out var ingredient))
                    errors.Add($"{path}: nieistniejący składnik.");
                else
                {
                    targetPath = $"ingredients[{target.Id}].amount";
                    if (ingredient.Amount?.Kind != "unknown")
                        errors.Add($"{path}: ilość przy nierozstrzygniętym konflikcie musi być unknown.");
                }
            }
            else
            {
                if (target.Id is not null) errors.Add($"{path}: globalny cel wymaga id=null.");
                switch (target.Type)
                {
                    case "servings":
                        targetPath = "servings";
                        if (recipe.Servings is not null) errors.Add($"{path}: porcje przy konflikcie muszą być null.");
                        break;
                    case "totalTime": targetPath = "times.total"; break;
                    case "preparationTime": targetPath = "times.preparation"; break;
                    case "cookingTime": targetPath = "times.cooking"; break;
                    case "restingTime": targetPath = "times.resting"; break;
                    default: errors.Add($"{path}: nieznany typ celu."); break;
                }
                MinuteRange? effective = target.Type switch
                {
                    "totalTime" => recipe.Times.Total, "preparationTime" => recipe.Times.Preparation,
                    "cookingTime" => recipe.Times.Cooking, "restingTime" => recipe.Times.Resting, _ => null
                };
                if (effective is not null) errors.Add($"{path}: czas przy konflikcie musi być null.");
            }
            if (targetPath is not null)
            {
                if (!unresolved.Add(targetPath)) errors.Add($"{path}: drugi konflikt dla tego samego pola.");
                missing.Add($"{path}: rozstrzygnij {targetPath} ({string.Join(" / ", conflict.Candidates?.Where(c => c is not null).Select(c => c.ValueText) ?? [])}).");
            }
        }

        if (string.IsNullOrWhiteSpace(recipe.Title)) missing.Add("title: podaj tytuł.");
        if (recipe.Servings is <= 0) errors.Add("servings musi być dodatnią liczbą całkowitą albo null.");
        if (recipe.Servings is null && !unresolved.Contains("servings")) missing.Add("servings: podaj liczbę porcji.");
        Duration(recipe.Times.Total, "times.total");
        Duration(recipe.Times.Preparation, "times.preparation");
        Duration(recipe.Times.Cooking, "times.cooking");
        Duration(recipe.Times.Resting, "times.resting");
        if (recipe.Times.Total is null && !unresolved.Contains("times.total")) missing.Add("times.total: podaj czas całkowity.");
        if (recipe.Ingredients.Length == 0) missing.Add("ingredients: brak składników.");

        foreach (var ingredient in recipe.Ingredients.Where(i => i is not null))
        {
            var path = $"ingredients[{ingredient.Id}]";
            if (string.IsNullOrWhiteSpace(ingredient.Name)) missing.Add($"{path}.name: podaj nazwę składnika.");
            if (ingredient.Qualifiers is null || ingredient.Qualifiers.Any(string.IsNullOrWhiteSpace))
                errors.Add($"{path}.qualifiers: wymagana lista niepustych opisów.");
            Evidence(ingredient.SourceText, path + ".sourceText");
            var amount = ingredient.Amount;
            if (amount is null) { errors.Add($"{path}.amount nie może być null."); continue; }
            var shapeOk = amount.Kind switch
            {
                "exact" => amount.Value is > 0 && amount.Min is null && amount.Max is null && amount.Text is null,
                "range" => amount.Value is null && amount.Min is > 0 && amount.Max >= amount.Min && amount.Text is null,
                "text" => amount.Value is null && amount.Min is null && amount.Max is null && amount.Unit is null && !string.IsNullOrWhiteSpace(amount.Text),
                "unknown" => amount.Value is null && amount.Min is null && amount.Max is null && amount.Text is null,
                _ => false
            };
            if (!shapeOk) errors.Add($"{path}.amount: pola nie odpowiadają kind={amount.Kind}.");
            if (amount.Unit is not null && string.IsNullOrWhiteSpace(amount.Unit)) errors.Add($"{path}.amount.unit: użyj null zamiast pustego tekstu.");
            if (ingredient.Optional || unresolved.Contains(path + ".amount")) continue;
            if (amount.Kind == "unknown") missing.Add($"{path}.amount: podaj ilość ({ingredient.Name ?? ingredient.Id}).");
            if (amount.Kind is "exact" or "range")
            {
                if (amount.Unit is null) missing.Add($"{path}.amount.unit: podaj jednostkę.");
                else if (!Units.Contains(amount.Unit)) review.Add($"{path}.amount.unit: jednostka {amount.Unit} nie jest jeszcze obsługiwana.");
            }
            if (amount.Kind == "text" && !string.Equals(amount.Text?.Trim(), "do smaku", StringComparison.OrdinalIgnoreCase))
                review.Add($"{path}.amount.text: ilość opisowa wymaga przeglądu ({amount.Text}).");
        }

        if (recipe.Steps.Length == 0) missing.Add("steps: brak kroków przygotowania.");
        var stepIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in recipe.Steps)
        {
            if (step is null) { errors.Add("steps zawiera null."); continue; }
            if (step.Id is null || !Regex.IsMatch(step.Id, "^s[1-9][0-9]*$") || !stepIds.Add(step.Id))
                errors.Add("Nieprawidłowy lub powtórzony identyfikator kroku.");
            if (string.IsNullOrWhiteSpace(step.Text)) errors.Add($"steps[{step.Id}].text: pusty krok.");
            Evidence(step.SourceText, $"steps[{step.Id}].sourceText");
            if (step.IngredientIds is null || step.IngredientIds.Any(id => id is null || !ingredients.ContainsKey(id)) ||
                step.IngredientIds.Distinct().Count() != step.IngredientIds.Length)
                errors.Add($"steps[{step.Id}].ingredientIds: nieistniejące lub powtórzone odniesienie.");
        }
        return Finish();
    }
}
