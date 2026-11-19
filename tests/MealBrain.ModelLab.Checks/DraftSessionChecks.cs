using MealBrain.ModelLab.Recipes;

// Sprawdzenia stanu szkicu na zapisanych odczytach S3 (regresja v2). Model nie jest wywoływany.
static class DraftSessionChecks
{
    public static void Run()
    {
        static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        static DraftSession Load(int id)
        {
            var prefix = Path.Combine(AppContext.BaseDirectory, "extraction-check-v2", $"extract-{id:000}");
            return new DraftSession(RecipeJson.Parse(File.ReadAllText(prefix + ".draft.json")),
                File.ReadAllText(prefix + ".source.txt"));
        }
        static AmountDraft Exact(decimal value, string unit) => new()
            { Kind = "exact", Value = value, Min = null, Max = null, Unit = unit, Text = null };

        // Ryż: kompletny szkic. „Jeszcze nie zatwierdzam” nie zmienia stanu; zgoda tylko jawnie, na bieżącą rewizję.
        var rice = Load(1);
        Check(rice.State == DraftState.ReadyForReview, $"Ryż: oczekiwano ReadyForReview, jest {rice.State}.");
        Check(rice.ApprovedRevision is null, "Ryż: zgoda nadana bez decyzji użytkowniczki.");
        Check(!rice.Approve(rice.Revision + 1, "Zatwierdzam").Applied, "Ryż: przyjęto zgodę na nieistniejącą rewizję.");
        Check(rice.Approve(rice.Revision, "Zatwierdzam ten przepis.").Applied && rice.State == DraftState.Approved,
            "Ryż: jawna zgoda nie została zapisana.");
        var riceIngredient = rice.Recipe!.Ingredients[0];
        Check(rice.SetIngredientAmount(riceIngredient.Id, Exact(250, "g"), "Zmień na 250 g.").Applied, "Ryż: odrzucono poprawną zmianę.");
        Check(rice.State == DraftState.ReadyForReview && rice.ApprovedRevision == 1,
            "Ryż: zmiana po zgodzie nie wymusiła ponownego przeglądu.");

        // Ogórki: brak porcji i ilości jogurtu. Uzupełniamy tylko te pola, sourceText zostaje.
        var cucumbers = Load(2);
        Check(cucumbers.State == DraftState.NeedsClarification, "Ogórki: braki nie zostały wykryte.");
        Check(!cucumbers.Approve(cucumbers.Revision, "Zatwierdzam").Applied, "Ogórki: zatwierdzono niekompletny szkic.");
        var yogurt = cucumbers.Recipe!.Ingredients.Single(i => i.Amount.Kind == "unknown");
        var yogurtSource = yogurt.SourceText;
        Check(!cucumbers.SetServings(0, "0 porcji").Applied, "Ogórki: przyjęto 0 porcji.");
        Check(cucumbers.SetServings(2, "2 porcje, 100 g jogurtu").Applied, "Ogórki: nie zapisano porcji.");
        Check(cucumbers.SetIngredientAmount(yogurt.Id, Exact(100, "g"), "2 porcje, 100 g jogurtu").Applied, "Ogórki: nie zapisano jogurtu.");
        var yogurtAfter = cucumbers.Recipe!.Ingredients.Single(i => i.Id == yogurt.Id);
        Check(yogurtAfter.SourceText == yogurtSource && yogurtAfter.Name == yogurt.Name, "Ogórki: zmieniono źródło lub nazwę.");
        Check(cucumbers.State == DraftState.ReadyForReview, $"Ogórki: po uzupełnieniu stan {cucumbers.State}.");
        Check(cucumbers.Revision == 3 && cucumbers.History.Count == 2, "Ogórki: historia nie odpowiada zmianom.");

        // Sos: konflikt 20/30 ml. Zwykła zmiana ilości jest zablokowana; rozstrzygnięcie usuwa konflikt i zachowuje jego historię.
        var sauce = Load(6);
        var conflict = sauce.Recipe!.Conflicts.Single();
        var juiceId = conflict.Target.Id!;
        Check(sauce.State == DraftState.NeedsClarification, "Sos: konflikt nie blokuje szkicu.");
        Check(!sauce.SetIngredientAmount(juiceId, Exact(30, "ml"), "30 ml").Applied, "Sos: ominięto konflikt zwykłą zmianą.");
        Check(sauce.ResolveAmountConflict(conflict.Id, Exact(30, "ml"), "Poprawne jest 30 ml.").Applied, "Sos: nie rozstrzygnięto konfliktu.");
        Check(sauce.Recipe!.Conflicts.Length == 0 && sauce.ResolvedConflicts.Single().Conflict == conflict,
            "Sos: konflikt nie przeszedł do historii.");
        Check(sauce.State == DraftState.ReadyForReview, $"Sos: po rozstrzygnięciu stan {sauce.State}.");

        // Kanapki: model zmienił cytat. Brak pustych pól nie wystarcza; potrzebne jawne przejrzenie uwagi.
        var sandwiches = Load(3);
        Check(sandwiches.Validation.MissingFields.Length == 0 && sandwiches.OpenReviewIssues.Length == 1,
            "Kanapki: oczekiwano jednej uwagi o cytacie i braku pustych pól.");
        Check(sandwiches.State == DraftState.NeedsClarification, "Kanapki: zmieniony cytat nie blokuje zgody.");
        Check(!sandwiches.Approve(sandwiches.Revision, "Zatwierdzam").Applied, "Kanapki: zatwierdzono bez przeglądu cytatu.");
        Check(sandwiches.AcknowledgeReviewIssue(sandwiches.OpenReviewIssues[0], "Widzę, to literówka, w porządku.").Applied,
            "Kanapki: nie zapisano przeglądu uwagi.");
        Check(sandwiches.State == DraftState.ReadyForReview && sandwiches.Approve(sandwiches.Revision, "Zatwierdzam.").Applied,
            "Kanapki: po przeglądzie nie można zatwierdzić.");

        Console.WriteLine("OK: stan szkicu — zgoda na konkretną rewizję, uzupełnienie braków, rozstrzygnięcie konfliktu i przegląd cytatu.");
    }
}
