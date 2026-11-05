namespace MealBrain.ModelLab.Recipes;

// Stan szkicu przepisu po imporcie (S1/S3) utrzymywany przez aplikację, nie przez model.
// Model może zaproponować zmianę, ale tylko ta klasa ją stosuje, waliduje i nadaje zgodę.
public enum DraftState { InvalidOutput, NotRecipe, NeedsClarification, ReadyForReview, Approved }

public sealed record DraftChange(
    int Revision, string Operation, string Target, string? OldValue, string? NewValue, string UserText);

public sealed record ResolvedConflict(int Revision, ConflictDraft Conflict, string ChosenValue, string UserText);

public sealed record DraftUpdateResult(bool Applied, string Message)
{
    public static DraftUpdateResult Ok(string message) => new(true, message);
    public static DraftUpdateResult Rejected(string message) => new(false, message);
}

public sealed class DraftSession
{
    private readonly List<DraftChange> history = [];
    private readonly List<ResolvedConflict> resolvedConflicts = [];
    private readonly HashSet<string> acknowledgedReviewIssues = new(StringComparer.Ordinal);

    public DraftSession(RecipeExtractionDocument document, string originalText)
    {
        Document = document;
        OriginalText = originalText;
    }

    // Oryginalne źródło nie zmienia się po imporcie. Wypowiedzi użytkowniczki trafiają do historii, nie tutaj.
    public string OriginalText { get; }
    public RecipeExtractionDocument Document { get; private set; }
    public RecipeDraft? Recipe => Document.Recipe;
    public int Revision { get; private set; } = 1;
    public int? ApprovedRevision { get; private set; }
    public IReadOnlyList<DraftChange> History => history;
    public IReadOnlyList<ResolvedConflict> ResolvedConflicts => resolvedConflicts;

    public DraftValidation Validation => RecipeDraftValidator.Validate(Document, OriginalText);

    // Uwagi walidatora (np. zmieniony cytat), których użytkowniczka jeszcze nie obejrzała.
    public string[] OpenReviewIssues => Validation.ReviewIssues.Where(i => !acknowledgedReviewIssues.Contains(i)).ToArray();

    public DraftState State
    {
        get
        {
            var validation = Validation;
            if (validation.Status == "invalid_output") return DraftState.InvalidOutput;
            if (validation.Status == "not_recipe") return DraftState.NotRecipe;
            if (validation.MissingFields.Length > 0 || OpenReviewIssues.Length > 0) return DraftState.NeedsClarification;
            return ApprovedRevision == Revision ? DraftState.Approved : DraftState.ReadyForReview;
        }
    }

    public DraftUpdateResult SetServings(int servings, string userText)
    {
        var recipe = Recipe;
        if (recipe is null) return DraftUpdateResult.Rejected("Ten import nie zawiera przepisu.");
        if (servings <= 0) return DraftUpdateResult.Rejected("Liczba porcji musi być dodatnia.");
        if (FindConflict(c => c.Target.Type == "servings") is { } conflict)
            return DraftUpdateResult.Rejected($"Porcje mają nierozstrzygnięty konflikt {conflict.Id}; użyj rozstrzygnięcia konfliktu.");
        return Apply(recipe with { Servings = servings }, "setServings", "servings",
            recipe.Servings?.ToString(), servings.ToString(), userText);
    }

    public DraftUpdateResult SetTotalTime(MinuteRange total, string userText)
    {
        var recipe = Recipe;
        if (recipe is null) return DraftUpdateResult.Rejected("Ten import nie zawiera przepisu.");
        if (FindConflict(c => c.Target.Type == "totalTime") is { } conflict)
            return DraftUpdateResult.Rejected($"Czas całkowity ma nierozstrzygnięty konflikt {conflict.Id}; użyj rozstrzygnięcia konfliktu.");
        return Apply(recipe with { Times = recipe.Times with { Total = total } }, "setTotalTime", "times.total",
            Describe(recipe.Times.Total), Describe(total), userText);
    }

    public DraftUpdateResult SetIngredientAmount(string ingredientId, AmountDraft amount, string userText)
    {
        var recipe = Recipe;
        if (recipe is null) return DraftUpdateResult.Rejected("Ten import nie zawiera przepisu.");
        var ingredient = recipe.Ingredients.FirstOrDefault(i => i.Id == ingredientId);
        if (ingredient is null) return DraftUpdateResult.Rejected($"Nie ma składnika {ingredientId}.");
        if (FindConflict(c => c.Target.Type == "ingredientAmount" && c.Target.Id == ingredientId) is { } conflict)
            return DraftUpdateResult.Rejected($"Składnik {ingredientId} ma nierozstrzygnięty konflikt {conflict.Id}; użyj rozstrzygnięcia konfliktu.");
        // Zachowujemy id, nazwę i sourceText; zmienia się tylko ilość.
        var updated = recipe with { Ingredients = recipe.Ingredients.Select(i => i.Id == ingredientId ? i with { Amount = amount } : i).ToArray() };
        return Apply(updated, "setIngredientAmount", $"ingredients[{ingredientId}].amount",
            Describe(ingredient.Amount), Describe(amount), userText);
    }

    public DraftUpdateResult ResolveAmountConflict(string conflictId, AmountDraft amount, string userText)
    {
        var recipe = Recipe;
        var conflict = recipe?.Conflicts.FirstOrDefault(c => c.Id == conflictId);
        if (recipe is null || conflict is null) return DraftUpdateResult.Rejected($"Nie ma konfliktu {conflictId}.");
        if (conflict.Target.Type != "ingredientAmount" || conflict.Target.Id is not { } ingredientId)
            return DraftUpdateResult.Rejected($"Konflikt {conflictId} nie dotyczy ilości składnika.");
        var old = recipe.Ingredients.First(i => i.Id == ingredientId).Amount;
        var updated = recipe with
        {
            Ingredients = recipe.Ingredients.Select(i => i.Id == ingredientId ? i with { Amount = amount } : i).ToArray(),
            Conflicts = recipe.Conflicts.Where(c => c.Id != conflictId).ToArray()
        };
        return Apply(updated, "resolveConflict", $"ingredients[{ingredientId}].amount",
            Describe(old), Describe(amount), userText, conflict);
    }

    public DraftUpdateResult ResolveServingsConflict(string conflictId, int servings, string userText)
    {
        var recipe = Recipe;
        var conflict = recipe?.Conflicts.FirstOrDefault(c => c.Id == conflictId);
        if (recipe is null || conflict is null) return DraftUpdateResult.Rejected($"Nie ma konfliktu {conflictId}.");
        if (conflict.Target.Type != "servings") return DraftUpdateResult.Rejected($"Konflikt {conflictId} nie dotyczy porcji.");
        if (servings <= 0) return DraftUpdateResult.Rejected("Liczba porcji musi być dodatnia.");
        var updated = recipe with { Servings = servings, Conflicts = recipe.Conflicts.Where(c => c.Id != conflictId).ToArray() };
        return Apply(updated, "resolveConflict", "servings", null, servings.ToString(), userText, conflict);
    }

    public DraftUpdateResult ResolveTimeConflict(string conflictId, MinuteRange time, string userText)
    {
        var recipe = Recipe;
        var conflict = recipe?.Conflicts.FirstOrDefault(c => c.Id == conflictId);
        if (recipe is null || conflict is null) return DraftUpdateResult.Rejected($"Nie ma konfliktu {conflictId}.");
        var times = recipe.Times;
        (RecipeTimes? newTimes, string path) = conflict.Target.Type switch
        {
            "totalTime" => (times with { Total = time }, "times.total"),
            "preparationTime" => (times with { Preparation = time }, "times.preparation"),
            "cookingTime" => (times with { Cooking = time }, "times.cooking"),
            "restingTime" => (times with { Resting = time }, "times.resting"),
            _ => ((RecipeTimes?)null, "")
        };
        if (newTimes is null) return DraftUpdateResult.Rejected($"Konflikt {conflictId} nie dotyczy czasu.");
        var updated = recipe with { Times = newTimes, Conflicts = recipe.Conflicts.Where(c => c.Id != conflictId).ToArray() };
        return Apply(updated, "resolveConflict", path, null, Describe(time), userText, conflict);
    }

    // Użytkowniczka obejrzała uwagę walidatora (np. różnicę cytatu) i akceptuje ją. Dane się nie zmieniają.
    public DraftUpdateResult AcknowledgeReviewIssue(string issue, string userText)
    {
        if (!Validation.ReviewIssues.Contains(issue)) return DraftUpdateResult.Rejected("Nie ma takiej uwagi do przeglądu.");
        if (!acknowledgedReviewIssues.Add(issue)) return DraftUpdateResult.Rejected("Ta uwaga została już przejrzana.");
        history.Add(new(Revision, "acknowledgeReview", issue, null, null, userText));
        return DraftUpdateResult.Ok("Uwaga oznaczona jako przejrzana.");
    }

    // Zgoda dotyczy konkretnej rewizji. Tekst modelu „zatwierdzono” nie wywołuje tej metody sam z siebie.
    public DraftUpdateResult Approve(int revision, string userText)
    {
        if (revision != Revision)
            return DraftUpdateResult.Rejected($"Zgoda dotyczy rewizji {revision}, a aktualna to {Revision}. Pokaż szkic ponownie.");
        var state = State;
        if (state == DraftState.Approved) return DraftUpdateResult.Rejected($"Rewizja {Revision} jest już zatwierdzona.");
        if (state != DraftState.ReadyForReview)
            return DraftUpdateResult.Rejected($"Nie można zatwierdzić w stanie {state}. Najpierw uzupełnij braki i przejrzyj uwagi.");
        ApprovedRevision = Revision;
        history.Add(new(Revision, "approve", $"revision {Revision}", null, null, userText));
        return DraftUpdateResult.Ok($"Zatwierdzono rewizję {Revision}.");
    }

    private DraftUpdateResult Apply(RecipeDraft updated, string operation, string target, string? oldValue, string? newValue,
        string userText, ConflictDraft? resolved = null)
    {
        var candidate = Document with { Recipe = updated };
        var validation = RecipeDraftValidator.Validate(candidate, OriginalText);
        if (validation.Status == "invalid_output")
            return DraftUpdateResult.Rejected("Zmiana dałaby niepoprawny szkic: " + string.Join(" ", validation.Errors));
        Document = candidate;
        Revision++;
        history.Add(new(Revision, operation, target, oldValue, newValue, userText));
        if (resolved is not null) resolvedConflicts.Add(new(Revision, resolved, newValue ?? "", userText));
        var approvalNote = ApprovedRevision is { } approved ? $" Zgoda dla rewizji {approved} nie obejmuje tej zmiany." : "";
        return DraftUpdateResult.Ok($"Zapisano {target} = {newValue} (rewizja {Revision}).{approvalNote}");
    }

    private ConflictDraft? FindConflict(Func<ConflictDraft, bool> predicate) => Recipe?.Conflicts.FirstOrDefault(predicate);

    public static string Describe(AmountDraft amount) => amount.Kind switch
    {
        "exact" => $"{amount.Value} {amount.Unit}".Trim(),
        "range" => $"{amount.Min}–{amount.Max} {amount.Unit}".Trim(),
        "text" => amount.Text ?? "",
        _ => "nieznana" + (amount.Unit is null ? "" : $" ({amount.Unit})")
    };

    public static string? Describe(MinuteRange? range) => range is null ? null
        : range.MinMinutes == range.MaxMinutes ? $"{range.MinMinutes} min" : $"{range.MinMinutes}–{range.MaxMinutes} min";
}
