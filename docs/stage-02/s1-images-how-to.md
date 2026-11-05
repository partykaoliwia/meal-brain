# S1 — import przepisu ze screena: instrukcja dla Patrycji

Stan: 5 listopada 2026. Wykonano [pierwsze porównanie sześciu screenów z Instagrama](reports/2026-11-05-s1-instagram-v1/report.md): Gemma 0/6, Qwen 1/6, przy czym zaliczenie dotyczy wyłącznie kadru bez przepisu. Wzorce są uzupełnione, zestaw zamrożony jako `s1-instagram-v1`. [Protokół](s1-instagram-v1-protocol.md) opisuje rzeczywiste obrazy, ich oryginalne nazwy i ograniczenia. Poniższa instrukcja zachowuje również plan przygotowania przyszłego rozszerzonego zestawu i osobnego pilotażu.

Wszystkie komendy poniżej uruchamiaj w PowerShellu z głównego katalogu `meal-brain`. Wymagane są .NET 10, działająca Ollama i modele obsługujące obrazy. Dotychczasowy import tekstowy i jego wyniki pozostają osobnym eksperymentem.

## 1. Modele

Para początkowa: zainstalowana `gemma3:4b` oraz `qwen3-vl:4b-instruct`. Zwykły `qwen3:4b` jest modelem tekstowym. Program sprawdza `vision` przez `/api/show`, zanim wyśle obraz. Wersja Qwen Instruct ogranicza zakres eksperymentu do odczytu, bez porównywania wariantów Thinking.

```powershell
ollama --version
ollama list
ollama pull qwen3-vl:4b-instruct
```

Pobieranie modelu wymaga kilku GB miejsca i dostępu do sieci. Narzędzia S1 nie pobierają ani nie aktualizują modeli automatycznie. Qwen3-VL wymaga Ollamy co najmniej 0.12.7. Modele uruchamiamy po kolei. Zapisane metadane zawierają zainstalowane modele z digestami i szczegóły badanego modelu.

## 2. Twoje zadanie teraz: screeny

Aktualne screeny są już w katalogu images pod oryginalnymi nazwami IMG_5280–IMG_5286, zgodnie z manifestem i protokołem. Tabela poniżej to pierwotny plan przyszłych dodatkowych przypadków, nie opis pokrycia przeprowadzonego testu. Nie zmieniaj istniejącego zamrożonego zestawu.

Dodaj sześć plików PNG do `evals/s1-images-v1/images/`:

| Plik | Co przygotować |
|---|---|
| `S1-001.png` | Czytelny, kompletny opis przepisu na Instagramie, z rozwiniętym opisem. |
| `S1-002.png` | Czytelny przepis ze strony, najlepiej z ułamkiem, zakresem lub składnikiem opcjonalnym. |
| `S1-003.png` | Rzeczywisty przepis bez porcji i co najmniej jednej ilości. |
| `S1-004.png` | Fragment przepisu z jedną nieczytelną lub uciętą ilością; reszta powinna być możliwa do odczytania. |
| `S1-005.png` | Przepis z konfliktem listy składników i kroku, np. 200 g wobec 250 g. Może być jawnie oznaczonym materiałem syntetycznym. |
| `S1-006.png` | Zdjęcie potrawy lub reklama bez treści przepisu. |

Dwa dodatkowe screeny do rozwijania rozwiązania dodaj do `evals/s1-images-v1/pilot/`, np. jako `pilot-01.png` i `pilot-02.png`. Obrazy z pilotażu nie są oceną końcową. Zachowaj oryginalną czytelność i ten sam plik dla obu modeli. Jeżeli potrzebny jest JPEG, zmień odpowiednie `image` w `suite.json` przed zamrożeniem. Obsługiwane wejścia to PNG/JPEG do 25 MB; program sprawdza sygnaturę pliku, a nie tylko rozszerzenie.

Jeżeli jeden przepis jest podzielony na kilka screenów, na ten pierwszy eksperyment wybierz inny lub przygotuj jeden czytelny screen. Wieloobrazowy import nie jest tu zaimplementowany. Zachowaj źródłowy adres do raportu, jeśli go masz; model nie dostaje linku i nie pobiera dodatkowej treści.

## 3. Pierwszy odczyt: pilot

Po dodaniu przykładowego screena możesz już wykonać:

```powershell
dotnet run --project .\src\MealBrain.ModelLab -- --extract-image gemma3:4b .\evals\s1-images-v1\pilot\pilot-01.png
dotnet run --project .\src\MealBrain.ModelLab -- --extract-image qwen3-vl:4b-instruct .\evals\s1-images-v1\pilot\pilot-01.png
```

Każde wywołanie tworzy nowy katalog `runs/*-s1-*`. Otwórz `PILOT-001/draft.json`, `validation.json` i zachowany `source.png` lub `source.jpg`. Plik `turns.jsonl` zachowuje surowe odpowiedzi API, także błędy. To najprostsza droga do sprawdzenia działania przed przygotowaniem pełnej oceny.

Prompt jest w `src/MealBrain.ModelLab/Recipes/image-extraction-prompt-v1.txt`. Jest kopiowany do katalogu programu podczas kompilacji. Po zmianie promptu ponownie użyj `dotnet run` bez `--no-build`. Przed właściwą próbą kończymy poprawki i nadajemy kolejną wersję warunków, jeśli zmieniamy je później.

## 4. Wzorce przygotowujemy na podstawie gotowych screenów

W `evals/s1-images-v1/references/` są sześć szablonów JSON. Możemy je uzupełnić po dodaniu screenów; Patrycja nie musi przygotowywać ich przed samymi obrazami. Każdy zawiera:

- `ready`: początkowo `false`; ustawiamy `true` po sprawdzeniu całego wzorca;
- `transcription`: ręczny odczyt widocznego tekstu; nieczytelne fragmenty jawnie opisujemy, bez zgadywania;
- `expectedDraft`: pełny dokument RecipeDraft v1, zgodny z widocznym źródłem;
- `expectedValidationStatus`: status z `RecipeDraftValidator.Validate(expectedDraft)`, bez argumentu tekstowego;
- `requiredChecks`: oczekiwania semantyczne przygotowane przed testem;
- `sourceUrl`, `notes`: pochodzenie, informacje o przygotowaniu materiału i niejednoznacznościach.

Wzorzec RecipeDraft można oprzeć na `docs/contracts/recipe-draft-v1.example.json` oraz opisie kontraktu. Nie porównujemy JSON-ów bajt po bajcie: identyfikatory, forma nazw i podział zdań mogą być równoważne znaczeniowo. Wzorzec ustala wymagane dane i braki. Zwykły brak ilości to `unknown`; „do smaku” to ilość opisowa; nieczytelna liczba pozostaje nieznana, nawet jeśli znamy pełny oryginalny post.

**Transkrypcje, expectedDraft i kryteria nie są przekazywane do modelu.** Ekstraktor otrzymuje wyłącznie stałą instrukcję, schemat i bajty obrazu. Nie poprawiamy wyniku modelu przed oceną.

## 5. Kontrola i zamrożenie zestawu

```powershell
dotnet run --project .\src\MealBrain.ModelLab -- --check-image-suite .\evals\s1-images-v1\suite.json
dotnet run --project .\src\MealBrain.ModelLab -- --freeze-image-suite .\evals\s1-images-v1\suite.json
```

Pierwsza komenda wypisuje wszystkie brakujące obrazy i nieuzupełnione wzorce. Dopóki ich nie dodamy, kod wyjścia 1 jest oczekiwany. Nie wywołuje Ollamy.

Druga tworzy `suite.lock.json`: hashe obrazów, wzorców, manifestu, promptu i schematu oraz wersję walidatora. Nie nadpisuje istniejącego protokołu. Uruchomienie zestawu sprawdza te warunki przed kontaktem z Ollamą. Zmieniony obraz, wzorzec, prompt lub ustawienia wymagają nowej wersji zestawu. Nie usuwamy protokołu tylko po to, aby ukryć zmianę warunków już wykonanej próby.

Ustawienia początkowe w `suite.json`: temperatura 0, kontekst 16384, limit odpowiedzi 4096. Pole `think` jest pomijane; `keep_alive=10m`. Nie ma rozgrzewki ani automatycznych ponowień. Czas obejmuje pełne żądanie `/api/chat`, a pierwszy przypadek może obejmować ładowanie. Te same ustawienia stosujemy do obu modeli. Jeżeli pilotaż ujawni problem z pamięcią, zmień warunki przed zamrożeniem i zapisz ograniczenie.

## 6. Właściwe porównanie

Najwygodniej uruchomić oba modele kolejno:

```powershell
.\scripts\compare-s1.ps1
```

Albo osobno:

```powershell
dotnet run --project .\src\MealBrain.ModelLab -- --image-suite gemma3:4b .\evals\s1-images-v1\suite.json
dotnet run --project .\src\MealBrain.ModelLab -- --image-suite qwen3-vl:4b-instruct .\evals\s1-images-v1\suite.json
```

Gdy model nie mieści się w pamięci po poprzedniej próbie, po jej zakończeniu możesz wykonać `ollama stop gemma3:4b` i dopiero rozpocząć Qwena. Zapisz to w raporcie. Każdy obraz zaczyna się od nowej historii z dwoma wiadomościami. Błąd HTTP jednego obrazu jest zapisywany i nie pomija pozostałych przypadków.

Wyniki w `runs/` są ignorowane przez Git. Każdy katalog zawiera prompt, schemat, metadane, zamrożony protokół, manifest, obraz każdego przypadku, jego wzorzec, wynikowy dokument i walidację. `turns.jsonl` zachowuje odpowiedź API, ustawienia żądania i hash obrazu. Duże base64 nie jest powielane w logu; dokładne bajty są w zachowanym pliku źródłowym. W razie błędu zachowywany jest także `error.txt`.

## 7. Ocena jakości po uruchomieniu

Uzupełnij `review.json`, porównując obrazy, wzorce i odpowiedzi. `scorecard.csv` jest alternatywną pustą tabelą do przeglądania w arkuszu; przy ocenie wybierz jeden plik jako główne źródło i nie mieszaj dwóch różnych ocen.

| Pole | Co oceniamy |
|---|---|
| `technicalCompleted` | Wypełnia program: czy API dostarczyło kompletną odpowiedź; nie jest oceną poprawności danych. |
| `validationStatus` | Wypełnia C#: struktura i reguły wspólnego kontraktu. |
| `ocrPass` | Ręcznie: czy widoczne nazwy, liczby i fragmenty odczytano wiernie. |
| `interpretationPass` | Ręcznie: czy dane mają poprawne znaczenie, typ ilości, czas, opcjonalność, konflikt i powiązania kroków. |
| `criticalError` | Ręcznie: czy dopisano lub zmieniono istotną ilość, porcje lub składnik bez źródła, wykonano polecenie ze screena albo nadano zgodę. |
| `passed` | Ręcznie: cały przypadek spełnia wcześniej zapisane kryteria i nie ma błędu krytycznego. |
| `notes` | Konkretny fragment obrazu, wynik i uzasadnienie. Zapisz błędy API i niejednoznaczności. |

Dla materiału bez przepisu oceń poprawne odrzucenie; `ocrPass` może pozostać null z uzasadnieniem, jeśli nie było tekstu do odczytania. Niekompletny przepis z prawidłowym `needs_clarification` może zaliczyć przypadek. Gotowy JSON i kod wyjścia 0 nie nadają żadnej oceny jakości. Program nie wypełnia pól ręcznych. Brak odpowiedzi albo `invalid_output` oznacza niezaliczenie wykonania; odnotuj je również w ocenie końcowej.

Na sześciu przykładach raportuj liczbę zaliczonych przypadków, rodzaje błędów i czasy. Nie deklaruj ogólnej skuteczności modelu. Jeżeli poprawisz prompt na podstawie tych wyników, zestaw staje się rozwojowy; późniejsza niezależna ocena wymaga nowych obrazów. Pierwszeństwo przy wyborze modelu mają brak zmyślonych danych i poprawność, potem szybkość. Możliwy wynik: żaden model nie spełnia wymagań.

## 8. Integracja z S3 i rozmową Marii

`RecipeImageExtractor` zwraca istniejący `ExtractionResult` i `RecipeExtractionDocument`. Wspólne klasy, JSON Schema i `RecipeDraftValidator` są użyte bez zmiany kontraktu. S1 wywołuje `Validate(document)` bez `originalText`: nie można porównać cytatu z pikselami przez `string.Contains`. Nawet `ready_for_review` oznacza tylko gotowość struktury do przeglądu; zgodność `sourceText` i danych z obrazem trzeba zweryfikować wizualnie.

Do dalszej pracy przekaż `draft.json`, `validation.json` oraz `source.json` wraz z obrazem. Obecny `DraftSession` wymaga tekstowego oryginału; obsługa źródła obrazowego i wizualnego przeglądu musi zostać jawnie dodana przy integracji rozmowy. Nie przekazuj nazwy pliku, base64 ani tekstu wygenerowanego przez model jako rzekomo niezależnie zweryfikowanego oryginału. Moduł S1 sam nie zatwierdza przepisu i nie zapisuje go do bazy.

## 9. Testy kodu i raport

```powershell
dotnet run --project .\tests\MealBrain.ModelLab.Checks
```

To kontrola mechaniki na atrapach HTTP i sztucznym obrazie: bajty wejściowe, wspólny schemat, izolacja importów, brak wycieku wzorców, zamrożenie warunków, błędy i zachowanie dowodów. Nie uruchamia modeli i nie mierzy jakości OCR.

Wyniki opisz według [szablonu raportu S1](s1-images-report-template.md). Wybrane kompletne katalogi `runs/` można później skopiować do nowego katalogu raportu, pozostawiając logi i odpowiedzi niezmienione. Nie dopisujemy wyniku S1 do raportów S3.

Źródła techniczne: [Ollama — obrazy](https://docs.ollama.com/capabilities/vision), [JSON Schema](https://docs.ollama.com/capabilities/structured-outputs), [Gemma 3 4B](https://ollama.com/library/gemma3:4b), [Qwen3-VL 4B Instruct](https://ollama.com/library/qwen3-vl:4b-instruct).
