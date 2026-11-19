using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace MealBrain.ModelLab.Recipes;

// Etap 3: narzędzia wystawione modelowi (tool calling, wzorzec z lab 04). Model wybiera narzędzie,
// ale wykonuje je aplikacja przez DraftSession, a błędy walidacji wracają do modelu jako wynik narzędzia.
public sealed class DraftTools(DraftSession session, Func<int, bool> confirmSave, Func<string> lastUserText)
{
    public string? SavedJson { get; private set; }
    public List<string> Trace { get; } = [];

    public IList<AITool> Create() =>
    [
        AIFunctionFactory.Create(GetDraft, "get_draft", "Zwraca aktualny szkic przepisu (rewizja, stan, składniki z id, czasy, braki)."),
        AIFunctionFactory.Create(SetIngredientAmount, "set_ingredient_amount",
            "Ustawia ilość istniejącego składnika. Podaj ingredientId oraz value i unit (np. 200 i g) albo text='do smaku'."),
        AIFunctionFactory.Create(AddIngredient, "add_ingredient", "Dodaje nowy składnik z konkretną ilością (value i unit) albo 'do smaku'."),
        AIFunctionFactory.Create(RemoveIngredient, "remove_ingredient", "Usuwa składnik o podanym ingredientId."),
        AIFunctionFactory.Create(SetServings, "set_servings", "Ustawia liczbę porcji (dodatnia liczba całkowita)."),
        AIFunctionFactory.Create(SetTime, "set_time", "Ustawia czas w minutach. field: total, preparation, cooking albo resting."),
        AIFunctionFactory.Create(SaveRecipe, "save_recipe", "Zapisuje przepis. Tylko gdy użytkowniczka poprosiła o zapis; aplikacja zapyta ją o zgodę.")
    ];

    private string Done(string tool, string result) { Trace.Add($"{tool} -> {result}"); return result; }

    public string GetDraft()
    {
        var recipe = session.Recipe;
        if (recipe is null) return Done("get_draft", "BŁĄD: to nie jest przepis.");
        var v = session.Validation;
        var lines = new List<string>
        {
            $"rewizja {session.Revision}, stan {session.State}",
            $"tytuł: {recipe.Title}", $"porcje: {recipe.Servings?.ToString() ?? "brak"}",
            $"czas: total={DraftSession.Describe(recipe.Times.Total) ?? "brak"}, preparation={DraftSession.Describe(recipe.Times.Preparation) ?? "brak"}, cooking={DraftSession.Describe(recipe.Times.Cooking) ?? "brak"}, resting={DraftSession.Describe(recipe.Times.Resting) ?? "brak"}",
            "składniki:"
        };
        lines.AddRange(recipe.Ingredients.Select(i => $"  {i.Id}: {i.Name} — {DraftSession.Describe(i.Amount)}{(i.Amount.Kind == "unknown" ? " [BRAK ILOŚCI]" : "")}"));
        lines.AddRange(v.MissingFields.Select(m => "brak: " + m));
        lines.AddRange(session.OpenReviewIssues.Select(m => "do przeglądu: " + m));
        return Done("get_draft", string.Join("\n", lines));
    }

    // Ścisła ilość (odpowiednik wariantów exact/text ze strict schema): liczba > 0 + znana jednostka albo „do smaku”.
    public static (AmountDraft? Amount, string? Error) BuildAmount(double? value, string? unit, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            if (value is not null || !string.IsNullOrWhiteSpace(unit))
                return (null, "BŁĄD: podaj albo value+unit, albo text, nie jedno i drugie.");
            if (!string.Equals(text.Trim(), "do smaku", StringComparison.OrdinalIgnoreCase))
                return (null, $"BŁĄD: ilość opisowa „{text.Trim()}” jest niedozwolona. Dozwolone tylko „do smaku”. Zapytaj użytkowniczkę o konkretną ilość (liczba i jednostka).");
            return (new AmountDraft { Kind = "text", Value = null, Min = null, Max = null, Unit = null, Text = "do smaku" }, null);
        }
        if (value is not > 0) return (null, "BŁĄD: ilość wymaga liczby większej od zera. Zapytaj użytkowniczkę o konkretną ilość.");
        if (string.IsNullOrWhiteSpace(unit)) return (null, "BŁĄD: ilość wymaga jednostki (np. g, ml, łyżka, szt.).");
        if (!RecipeDraftValidator.IsSupportedUnit(unit.Trim()))
            return (null, $"BŁĄD: jednostka „{unit.Trim()}” nie jest obsługiwana. Obsługiwane: {string.Join(", ", RecipeDraftValidator.SupportedUnits.Order())}.");
        return (new AmountDraft { Kind = "exact", Value = (decimal)value, Min = null, Max = null, Unit = unit.Trim(), Text = null }, null);
    }

    public string SetIngredientAmount(
        [Description("id składnika, np. i2")] string ingredientId,
        [Description("liczba, np. 200")] double? value = null,
        [Description("jednostka, np. g")] string? unit = null,
        [Description("tylko 'do smaku'")] string? text = null)
    {
        var (amount, error) = BuildAmount(value, unit, text);
        if (amount is null) return Done("set_ingredient_amount", error!);
        var r = session.SetIngredientAmount(ingredientId, amount, lastUserText());
        return Done("set_ingredient_amount", r.Applied ? "OK: " + r.Message : "BŁĄD: " + r.Message);
    }

    public string AddIngredient(
        [Description("nazwa składnika")] string name,
        [Description("liczba")] double? value = null,
        [Description("jednostka")] string? unit = null,
        [Description("tylko 'do smaku'")] string? text = null)
    {
        var (amount, error) = BuildAmount(value, unit, text);
        if (amount is null) return Done("add_ingredient", error!);
        var r = session.AddIngredient(name, amount, false, lastUserText());
        return Done("add_ingredient", r.Applied ? "OK: " + r.Message : "BŁĄD: " + r.Message);
    }

    public string RemoveIngredient([Description("id składnika, np. i3")] string ingredientId)
    {
        var r = session.RemoveIngredient(ingredientId, lastUserText());
        return Done("remove_ingredient", r.Applied ? "OK: " + r.Message : "BŁĄD: " + r.Message);
    }

    public string SetServings([Description("liczba porcji")] int servings)
    {
        var r = session.SetServings(servings, lastUserText());
        return Done("set_servings", r.Applied ? "OK: " + r.Message : "BŁĄD: " + r.Message);
    }

    public string SetTime(
        [Description("total, preparation, cooking albo resting")] string field,
        [Description("minuty")] double minutes)
    {
        if (minutes < 0 || minutes > 24 * 60) return Done("set_time", "BŁĄD: czas musi mieścić się w przedziale 0–1440 minut.");
        var r = session.SetTime(field, new MinuteRange { MinMinutes = (decimal)minutes, MaxMinutes = (decimal)minutes }, lastUserText());
        return Done("set_time", r.Applied ? "OK: " + r.Message : "BŁĄD: " + r.Message);
    }

    public string SaveRecipe()
    {
        // Zgoda jest po stronie aplikacji: sama prośba modelu niczego nie zapisuje.
        var state = session.State;
        if (state != DraftState.ReadyForReview)
            return Done("save_recipe", $"BŁĄD: nie można zapisać w stanie {state}. " + string.Join(" ", session.Validation.MissingFields.Concat(session.OpenReviewIssues)));
        if (!confirmSave(session.Revision))
            return Done("save_recipe", "ODMOWA: użytkowniczka nie wyraziła zgody. Przepis nie został zapisany.");
        var r = session.Approve(session.Revision, lastUserText());
        if (!r.Applied) return Done("save_recipe", "BŁĄD: " + r.Message);
        SavedJson = JsonSerializer.Serialize(session.Document, RecipeJson.Options);
        return Done("save_recipe", $"OK: przepis zapisany (rewizja {session.Revision}).");
    }
}
