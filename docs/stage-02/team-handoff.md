# Etap 2 — przekazanie S3 i zadania zespołu

Stan: 4 listopada 2026. Oliwia zamyka bieżącą serię badań S3: [podsumowanie](s3-stage-02-summary.md), [ostatni wynik 5/6](reports/2026-11-04-s3-extraction-check-v2/report.md). Dostępny jest prototyp odczytu tekstu ze znanymi ograniczeniami. Na zajęciach zespół może przedstawić eksperymenty, wyniki i nieukończone elementy zgodnie z uzgodnionym zakresem etapu.

## Wspólny punkt wyjścia

Najpierw przeczytać [RecipeDraft v1](../contracts/recipe-draft-v1.md) i obejrzeć [przykład JSON](../contracts/recipe-draft-v1.example.json). To propozycja wspólnego kontraktu S1/S3, do uzgodnienia przed rozbudową obu modułów. Odnośniki do kodu:

- [RecipeDraft.cs](../../src/MealBrain.ModelLab/Recipes/RecipeDraft.cs): wspólne typy oraz `RecipeJson.Parse`.
- [RecipeDraftValidator.cs](../../src/MealBrain.ModelLab/Recipes/RecipeDraftValidator.cs): reguły i kompletność, wersja v3.
- [RecipeExtractor.cs](../../src/MealBrain.ModelLab/Recipes/RecipeExtractor.cs): tekstowy odczyt przez Ollamę.
- [extraction-prompt-v3.txt](../../src/MealBrain.ModelLab/Recipes/extraction-prompt-v3.txt): bieżąca instrukcja S3.
- [JSON Schema](../contracts/recipe-draft-v1.schema.json): wspólny kształt odpowiedzi.

Szkic zawiera dane i konflikty. Stan rozmowy, historia zmian i zgoda należą do aplikacji. `approved` nie jest polem odpowiedzi ekstraktora.

## Co przekazuje Oliwia

Porównanie modeli, dane testowe, instrukcje i pełne logi; odczyt tekst → dokument; kontrolę typów, ilości, identyfikatorów, konfliktów i dosłowności cytatów; listy braków i uwag; konsolę ręczną i automatyczne zestawy. Wybór roboczy: `gemma4:12b`, instrukcja ekstrakcji v3, schemat 1, walidator v3.

Otwarty problem ostatniej próby: model zmienia „Rozsmaruj…” na „Rozsmarzuj…” w cytacie. C# to oznacza. Próg tego testu 6/6 nie został spełniony. Walidacja nie wykrywa wszystkich błędów znaczenia, a odczyt przepisu często trwa około minuty po rozgrzaniu modelu. Nazwy składników nie są jeszcze ujednolicone do zakupów.

Dostępny moduł nie obsługuje uzupełniania w rozmowie, zgody, bazy ani obrazów. Dawne rozmowy z modelem są materiałem badawczym, odrębnym od nowego przepływu na szkicu.

## Maria — eksperyment rozmowy na szkicu

Możesz zacząć od zapisanych JSON-ów, bez czekania na modele i bez ponawiania całego porównania S3. Proponowany zakres na etap 2 to mała konsola lub próba dialogu, z raportem pokazującym także błędy. Punkt startowy:

| Przypadek | Gotowy szkic i źródło | Co sprawdzić |
|---|---|---|
| Kompletny przepis | [ryż JSON](reports/2026-11-04-s3-extraction-check-v2/evidence/extract-001.draft.json), [źródło](reports/2026-11-04-s3-extraction-check-v2/evidence/extract-001.source.txt) | Pokazanie szkicu do przeglądu; odpowiedź „jeszcze nie zatwierdzam” nie nadaje zgody. |
| Dwa braki | [ogórki JSON](reports/2026-11-04-s3-extraction-check-v2/evidence/extract-002.draft.json), [źródło](reports/2026-11-04-s3-extraction-check-v2/evidence/extract-002.source.txt) | Pytanie o porcje i ilość jogurtu; „2 porcje, 100 g jogurtu” uzupełnia tylko te pola. Ponowna walidacja. |
| Konflikt | [sos JSON](reports/2026-11-04-s3-extraction-check-v2/evidence/extract-006.draft.json), [źródło](reports/2026-11-04-s3-extraction-check-v2/evidence/extract-006.source.txt) | Pokazać 20/30 ml; „poprawne jest 30 ml” uzupełnia ilość soku i usuwa aktywny konflikt, zachowując jego historię. |
| Uwaga o źródle | [kanapki JSON](reports/2026-11-04-s3-extraction-check-v2/evidence/extract-003.draft.json), [źródło](reports/2026-11-04-s3-extraction-check-v2/evidence/extract-003.source.txt) | Pokazać rozbieżność cytatu do przeglądu. Sam brak pustych pól nie daje kompletnego, sprawdzonego odczytu. |

Szkic wczytasz przez `RecipeJson.Parse(json)`, a ponowną kontrolę wykonasz przez `RecipeDraftValidator.Validate(document, originalText)`.

Przy aktualizacjach zachowuj identyfikatory składników i oryginalne `sourceText`. Ilość podana później przez użytkowniczkę ma dowód w historii jej zmian, a nie w pierwotnym cytacie. Nie dodawaj nowych wypowiedzi do oryginalnego źródła, żeby sprawiały wrażenie danych przepisu. Lista `MissingFields` jest tekstem do prezentacji; aktualizacje opieraj na strukturach i identyfikatorach szkicu.

Docelowy przebieg: braki/konflikt → uzupełnienie → walidacja → przegląd → jawna zgoda na konkretną rewizję. Zmiana po zgodzie wymaga ponownego przeglądu. Na etap 2 można sprawdzić ten mechanizm na szkicu w pamięci i opisać nieukończone części. Zapis do bazy pozostaje osobną operacją późniejszego rozwoju.

Wynik badań S3 dotyczy ekstrakcji; jakość rozmowy trzeba ocenić osobno. Raport Marii powinien podać instrukcję, przekazywany kontekst (aktualny szkic i problemy), wypowiedzi testowe, odpowiedzi i wnioski. Nie musi odtwarzać dużej macierzy modeli Oliwii.

## Patrycja — eksperyment S1 z obrazami

Proponowany zakres na etap 2: dwa modele obsługujące obrazy, mały wspólny zestaw np. sześciu obrazów i zapis wyników. Można wykorzystać przepisy z [dotychczasowych przypadków](test-cases.md), przygotowując ręczny wzorzec przed uruchomieniem modeli. Uwzględnij czytelny kompletny przepis, niekompletne dane i trudniejszy odczyt zdjęcia. Warunki porównania powinny być jednakowe; nie trzeba zaczynać od wielu rodzin modeli.

Cel wyniku: dokument zgodny z tym samym RecipeDraft v1, z `unknown`/`null` przy nieczytelnych danych i bez zgadywania porcji lub ilości. W raporcie odróżnij odczyt tekstu obrazu od interpretacji składników, czasów i konfliktów. Zapisz ustawienia, oryginalny obraz, odpowiedź, ocenę i czas; nieudane próby też są wynikiem.

Obecny `RecipeExtractor` przyjmuje tekst i nie wysyła obrazów. Potrzebne jest osobne wywołanie modelu dla S1. Możesz wykorzystać wspólne klasy, `RecipeJson.Parse`, schemat i walidator. Instrukcję S1 przygotuj dla źródła obrazowego i wersjonuj oddzielnie od S3.

`sourceText` w S1 oznacza transkrypcję fragmentu obrazu. `Validate(document)` sprawdzi reguły danych, ale bez tekstowego oryginału nie zweryfikuje dosłowności cytatu względem obrazu. Nie przekazuj nazwy pliku ani base64 jako `originalText`. Jeżeli dodasz OCR, sprawdzenie cytatu względem OCR nadal wymaga osobnej oceny zgodności z obrazem.

Na zajęcia wystarczy czytelny opis eksperymentu i jego ograniczeń; pełny moduł importu obrazów można rozwijać później.

## Uruchomienie istniejącego S3

Wymagane: .NET 10, uruchomiona Ollama i pobrany model `gemma4:12b`. Z katalogu repozytorium:

```powershell
dotnet run --project .\src\MealBrain.ModelLab -- --extract gemma4:12b
```

Każde wejście jest niezależnym importem. `/case MB-001` wybiera przykład, `/paste` przyjmuje tekst wielowierszowy zakończony `/send`, `/exit` kończy program. Ten tryb nie kontynuuje rozmowy o poprzednim szkicu. Wyniki są w `runs/`; zachowane raporty i dowody w `docs/stage-02/reports/`.

Do demonstracji już uzyskanych wyników można pokazać zapisane szkice i logi z tabeli powyżej. Nie trzeba ponawiać wszystkich modeli. Pokaz archiwalnego wyniku należy wyraźnie tak nazwać.

Sprawdzenie mechaniki aplikacji bez rzeczywistych modeli: `dotnet run --project .\tests\MealBrain.ModelLab.Checks`. Domyślne przywracanie zależności jest potrzebne również po usunięciu katalogów `obj` lub przy pierwszym uruchomieniu ze świeżego repozytorium. Zestaw przeszedł podczas przekazania 4 listopada 2026; jego wynik nie jest oceną jakości Gemmy.

## Wywołanie z kodu hosta Marii

Wspólne typy i ekstraktor są publiczne w `MealBrain.ModelLab.Recipes`. Przy pracy w tym samym projekcie pliki promptu i schematu są kopiowane do katalogu wynikowego. Przy osobnym projekcie .NET 10 dodaj referencję projektu do `MealBrain.ModelLab` i zapewnij obecność obu plików w katalogu hosta `AppContext.BaseDirectory`.

Przykład wykonuje rzeczywiste wywołanie Ollamy:

```csharp
using System.Text.Json;
using MealBrain.ModelLab.Recipes;

using var http = new HttpClient
{
    BaseAddress = new Uri("http://localhost:11434"),
    Timeout = TimeSpan.FromMinutes(10)
};
var prompt = await File.ReadAllTextAsync(
    Path.Combine(AppContext.BaseDirectory, "extraction-prompt-v3.txt"));
var schema = JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(
    Path.Combine(AppContext.BaseDirectory, "recipe-draft-v1.schema.json")));
var extractor = new RecipeExtractor(http, "gemma4:12b", prompt, schema,
    entry => File.AppendAllTextAsync("s3-integration.jsonl",
        JsonSerializer.Serialize(entry) + Environment.NewLine));

var text = "Sałatka pomidorowa. 2 porcje. Całość: 5 minut. "
    + "Składniki: 200 g pomidorów, 1 łyżka oliwy. "
    + "Pokrój pomidory. Wymieszaj z oliwą.";
var result = await extractor.ExtractRecipeAsync(text);
Console.WriteLine(JsonSerializer.Serialize(result.Validation, RecipeJson.Options));
// result.Document zawiera odczyt, jeśli deserializacja się udała.
```

`ExtractionResult` zwraca dokument, walidację i czas. Samo `Document != null` nie oznacza poprawnego szkicu. Obsługa wyniku:

| Wynik | Znaczenie dla hosta |
|---|---|
| `invalid_output` | Zachować dowód błędu; dokumentu nie przyjmować jako poprawnego odczytu. |
| `not_recipe` | Pokazać odrzucenie; nie zadawać pytań o porcje reklamy. |
| `needs_clarification` | Pokazać braki, konflikty i uwagi; skierować do uzupełnienia lub przeglądu. |
| `ready_for_review` | Pokazać szkic do porównania ze źródłem; zgoda pozostaje odrębnym stanem aplikacji. |

Błędy HTTP, timeout, niepoprawna odpowiedź API i ucięcie mogą zostać rzucone jako wyjątki; host powinien je obsłużyć. Ekstraktor nie robi automatycznych ponowień.

## Co przygotować wspólnie na zajęcia

Podsumowanie wyboru modeli i przykład błędu z S3; wyniki prób obrazów Patrycji; przykład uzupełnienia lub próby rozmowy Marii; wspólny schemat przepływu oraz lista dalszych prac. Zakres przyszłej integracji, tool calling, MCP i RAG pozostaje zgodny z wizją projektu. Ten dokument nie potwierdza ukończenia całego etapu przez zespół ani oceny prowadzącego.
