# S3 — pierwszy odczyt do RecipeDraft

**Aktualizacja po wykonaniu próby:** [raport trzech importów](reports/2026-11-04-s3-extraction-v1/report.md) jest gotowy. Poniższy opis zachowuje protokół pierwszego wariantu; bieżąca komenda `--extract` uruchamia już `s3-extraction-v2`. Obecne zadanie i oczekiwania są w [instrukcji v2](s3-extraction-v2.md).

Przygotowano osobny wariant `s3-extraction-v1`: tekst → JSON zgodny z kontraktem → klasy C# → kontrola danych i wyliczenie braków. To prototyp modułu S3 do późniejszego połączenia z rozmową Marii oraz odczytem obrazów Patrycji.

Dotychczasowy prompt v2 i tryby porównywania modeli pozostają oddzielne. Ten wariant ma nową instrukcję, JSON Schema i limit odpowiedzi 4096 zamiast 2048 tokenów. Jego wyników nie wpisujemy jako kolejnego wykonania starych testów swobodnej rozmowy.

## Twoje pierwsze zadanie

W PowerShellu:

```powershell
Set-Location 'C:\Users\oliwi\Desktop\AI\meal-brain'
dotnet run --project .\src\MealBrain.ModelLab -- --extract gemma4:12b
```

W programie przy `Tekst>`:

```text
/case MB-002
```

Program wyśle tylko wejście przepisu, z osobną instrukcją ekstrakcji. Czekaj na pełny JSON. Nie wpisuj kolejnych odpowiedzi, dopóki wynik nie wróci. Rozbudowany JSON i schemat mogą wydłużyć pierwszą próbę; limit jednego żądania to 10 minut. Nie ma automatycznych ponowień ani naprawiania odpowiedzi po cichu.

## Czego oczekujemy w MB-002

W JSON:

- `inputKind: recipe` i `rejectionReason: null`.
- Tytuł „Makaron ze szpinakiem”, `servings: null`, czas całkowity 20–20 min.
- Pięć składników: makaron, 200 g szpinaku, śmietanka, 1 łyżka oliwy, sól do smaku.
- Makaron i śmietanka z `amount.kind: unknown`; sól z `amount.kind: text` i `text: do smaku`.
- Kroki z poprawnymi odniesieniami do składników, `conflicts: []`.

W wyniku wyliczonym przez C# oczekujemy `needs_clarification` i trzech braków: porcji, ilości makaronu i ilości śmietanki. Nazwy składników są w JSON przy ich identyfikatorach; lista braków wskazuje te same identyfikatory. Sól i osobny czas gotowania nie powinny blokować przeglądu.

Sprawdź też ręcznie, czy żaden składnik nie został pominięty. Kod nie wykryje automatycznie każdego pominięcia źródła przez model.

**To tryb odczytu, każda wiadomość jest nowym importem.** Nie wpisuj tu „180 g makaronu” ani „Zatwierdzam” jako kontynuacji poprzedniego przepisu. Uzupełnienia i kontrola zgody to następny moduł. Teraz sprawdzamy, czy odczyt daje poprawny szkic i czy kod prawidłowo wskazuje jego braki.

## Druga próba: konflikt

Następnie wpisz:

```text
/case MB-005
```

Oczekujemy:

- 2 porcje, 15 minut, pięć składników, w tym 5 g czosnku z kroku.
- Ilość pomidorów `unknown` z jednostką g.
- Jeden konflikt wskazujący identyfikator pomidorów, z wariantami 150 g i 200 g oraz fragmentami źródła.
- Brak konfliktu mozzarelli; jej ilość to 125 g.
- Kroki bieżące nie kopiują całej liczbowej ilości pomidorów, lecz wskazują składnik przez `ingredientIds`. Oryginalny krok z 200 g pozostaje w `sourceText`.
- Stan `needs_clarification`, z potrzebą rozstrzygnięcia konfliktu. Kod nie wybiera ilości za użytkowniczkę.

Zakończ:

```text
/exit
```

Zachowaj ścieżkę do katalogu wyników. Te dwie próby wystarczą do sprawdzenia nowego przepływu przed szerszym testowaniem.

## Własny opis

Komenda `/paste` rozpoczyna wiadomość wielowierszową. Wklej tekst przepisu i zakończ osobną linią `/send`. Możesz też wpisać krótkie wejście w jednej linii. Każde wywołanie API ma tylko instrukcję i bieżący tekst, bez historii innych importów.

## Co robi program

1. Przekazuje [JSON Schema](../contracts/recipe-draft-v1.schema.json) w polu `format` API Ollamy i opisuje ten sam schemat w instrukcji.
2. Pobiera wyłącznie końcową odpowiedź `message.content`; nie przekazuje treści thinking do innych wywołań.
3. Odczytuje JSON do klas C#, odrzucając brakujące wymagane pola, dodatkowe pola takie jak `approved`, powtórzone klucze i niezgodne typy.
4. Sprawdza reguły ilości, dodatnie porcje, zakresy, unikalne identyfikatory, odniesienia kroków i konflikty.
5. Wylicza kompletność w kodzie. Dla odczytu S3 sprawdza też, czy `sourceText` jest dosłownym fragmentem wejścia. Rozbieżności kieruje do przeglądu.

Wynik walidacji to:

- `invalid_output`: odpowiedź ma nieprawidłową strukturę lub sprzeczne z kontraktem wartości; nie traktujemy jej jako poprawnego szkicu.
- `not_recipe`: model odrzucił materiał i podał powód zgodny z kształtem kontraktu; prawidłowość tej decyzji nadal oceniamy względem źródła.
- `needs_clarification`: odczyt ma braki, konflikty albo informacje wymagające przeglądu.
- `ready_for_review`: kontrole kodu nie znalazły blokad; człowiek nadal przegląda zgodność z oryginałem.

Ten moduł nie zwraca `approved` i nie ma operacji zapisu przepisu do bazy. Walidacja nie dowodzi prawdziwości ilości ani kompletności ekstrakcji. Nawet dosłowny cytat nie dowodzi, że dana liczba pochodzi z właściwego miejsca, a nie np. z etykiety lub komentarza. Nie sprawdzamy tu pełnego bezpieczeństwa systemu.

## Pliki wynikowe

Każdy import otrzymuje numer w katalogu `runs/<data>-extract-<id>`:

- `metadata.json`: wariant, model, ustawienia, pełna instrukcja i schemat z hashami, środowisko.
- `extract-001.source.txt`: tekst podany w pierwszym imporcie.
- `extract-001.draft.json`: odczytany obiekt, jeśli udało się go zdeserializować. Może nadal wymagać poprawy lub mieć błędy reguł — sprawdź walidację.
- `extract-001.validation.json`: stan i listy błędów, braków i uwag do przeglądu.
- `turns.jsonl`: pełne żądania i odpowiedzi albo błędy. Każda linia ma `input_id` i `entry`, w którym są `kind`, `request`, `response` i walidacja lub błąd techniczny.

Oryginalna odpowiedź modelu jest w `entry.response.message.content`; plik draft jest ponownie zserializowanym obiektem. Błędnego formatu nie naprawiamy automatycznie. Niekompletna odpowiedź HTTP lub osiągnięcie limitu trafia do logu jako błąd, bez udawania pełnego importu.

## Co warto przeczytać w kodzie

- `Recipes/RecipeDraft.cs`: ustalony kształt danych oraz reguły deserializacji.
- `Recipes/RecipeExtractor.cs`: funkcja `ExtractRecipeAsync`, żądanie Ollamy i połączenie odczytu z walidacją. Zwraca `ExtractionResult`, czyli dokument (jeśli odczytany), walidację i czas, bo wynik może być niepełny lub nie być przepisem.
- `Recipes/RecipeDraftValidator.cs`: reguły zapisane w C#, niezależne od prośby modelu o dopytanie.
- `Recipes/ExtractionConsole.cs`: wczytanie tekstu, wywołanie funkcji i prezentacja plików.

Technika API: [structured outputs w Ollamie](https://docs.ollama.com/capabilities/structured-outputs).

Testy bez rzeczywistych modeli:

```powershell
dotnet run --project .\tests\MealBrain.ModelLab.Checks
```

Sprawdzają zarówno dotychczasowy runner testów, jak i nową strukturę, reguły danych, odrzucenie `approved`, konflikty, izolację importów, przekazanie schematu do API i reakcję na ucięcie odpowiedzi. Wyników tych sprawdzeń nie zaliczamy jako pomiaru jakości Gemmy.
