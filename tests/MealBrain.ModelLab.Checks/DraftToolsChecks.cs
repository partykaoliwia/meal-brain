using MealBrain.ModelLab.Recipes;

// Sprawdzenia narzędzi Etapu 3 bez modelu: błędy wracają jako tekst, szkic nie ulega zniszczeniu.
static class DraftToolsChecks
{
    public static void Run()
    {
        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        var prefix = Path.Combine(AppContext.BaseDirectory, "extraction-check-v2", "extract-002");
        var session = new DraftSession(RecipeJson.Parse(File.ReadAllText(prefix + ".draft.json")), File.ReadAllText(prefix + ".source.txt"));
        var consent = false;
        var tools = new DraftTools(session, _ => consent, () => "test");

        Check(tools.GetDraft().Contains("[BRAK ILOŚCI]"), "get_draft nie pokazuje braku ilości.");
        var rev = session.Revision;
        foreach (var bad in new[] { tools.SetIngredientAmount("i2", text: "tyle co zwykle"), tools.SetIngredientAmount("i2", value: 0, unit: "g"),
            tools.SetIngredientAmount("i2", value: 100), tools.SetIngredientAmount("i2", value: 100, unit: "kilogramoidy"),
            tools.SetIngredientAmount("i99", value: 100, unit: "g"), tools.SetTime("total", -5), tools.SetTime("nope", 5) })
            Check(bad.StartsWith("BŁĄD"), "Błędne wywołanie nie zwróciło BŁĄD: " + bad);
        Check(session.Revision == rev, "Odrzucone wywołanie zmieniło rewizję.");

        Check(tools.SaveRecipe().StartsWith("BŁĄD"), "Zapisano niekompletny szkic.");
        Check(tools.SetIngredientAmount("i2", value: 100, unit: "g").StartsWith("OK"), "Odrzucono poprawną ilość.");
        Check(tools.SetIngredientAmount("i3", text: "do smaku").StartsWith("OK"), "Odrzucono „do smaku”.");
        Check(tools.SetServings(0).StartsWith("BŁĄD"), "Przyjęto 0 porcji.");
        Check(tools.SetServings(2).StartsWith("OK"), "Nie ustawiono porcji.");
        Check(tools.AddIngredient("sól", 1, "łyżeczka").StartsWith("OK"), "Nie dodano składnika.");
        Check(tools.AddIngredient("Sól", 1, "łyżeczka").StartsWith("BŁĄD"), "Dodano duplikat.");
        Check(tools.RemoveIngredient("i4").StartsWith("OK"), "Nie usunięto składnika.");
        Check(tools.SetTime("total", 10).StartsWith("OK"), "Nie ustawiono czasu.");
        Check(session.State == DraftState.ReadyForReview, $"Oczekiwano ReadyForReview, jest {session.State}.");
        Check(tools.SaveRecipe().StartsWith("ODMOWA") && session.ApprovedRevision is null, "Zapis bez zgody.");
        consent = true;
        Check(tools.SaveRecipe().StartsWith("OK") && session.State == DraftState.Approved && tools.SavedJson is not null, "Zapis ze zgodą nie zadziałał.");
    }
}
