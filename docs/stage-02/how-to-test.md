# Etap 2 — jak technicznie uruchomić testy

## Co uruchamiamy

Korzystamy z małej aplikacji konsolowej C#, przygotowanej w `src/MealBrain.ModelLab`. Nie potrzebujecie tworzyć jej od zera ani instalować pakietów NuGet. Wymaga .NET SDK 10 i działającej Ollamy z pobranym modelem.

```text
Wy: wybór przypadku i późniejsze odpowiedzi
    ↓
Konsola C#: instrukcja + historia + nowa wiadomość
    ↓ HTTP POST http://localhost:11434/api/chat
Ollama → lokalny model
    ↓ odpowiedź + statystyki czasu
Konsola: wyświetlenie odpowiedzi i zapis logu
    ↓
Wy: porównanie z oczekiwanym wynikiem i wpis do raportu
```

To pierwsza wersja tekstowego asystenta i stanowisko do ręcznej oceny. Nie ma jeszcze narzędzi, bazy, RAG, obsługi obrazów ani automatycznego oceniania jakości. Odpowiedzi są zwykłym tekstem; pola JSON w szablonie wyników oznaczcie „nie dotyczy”.

Po analizie pierwszego przebiegu przygotowano instrukcję systemową v2. Najbliższy krok opisuje [eksperyment v1 → v2](prompt-experiment-v2.md): powtórzenie trzech przypadków na tym samym modelu i ustawieniach.

Aktualizacja: porównanie czterech modeli zakończono. Nowe testy Gemmy E2B i Qwena 9B uruchamiaj według [próby finalistów v1](finalists-v1.md), w trybie `--suite finalists-v1`. Poniższa instrukcja dotyczy interaktywnego zestawu MB-001–MB-006.

## 1. Sprawdźcie instalację

Otwórzcie PowerShell w katalogu repozytorium:

```powershell
Set-Location 'C:\Users\oliwi\Desktop\AI\meal-brain'
dotnet --list-sdks
ollama --version
```

Na innych komputerach podajcie własną ścieżkę do MealBrAIn. Potrzebny jest SDK `10.0.x`; na komputerze Oliwii jest już zainstalowany. Jeśli czegoś brakuje, zainstalujcie [Ollamę](https://ollama.com/download) lub [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0).

Otwórzcie aplikację Ollama i sprawdźcie serwer:

```powershell
ollama list
```

Jeżeli komenda zgłasza brak połączenia, uruchomcie w osobnym terminalu:

```powershell
ollama serve
```

Ten terminal zostaje otwarty. Jeżeli serwer już działa, nie uruchamiajcie drugiej kopii; komunikat o zajętym porcie zwykle wskazuje działającą pierwszą kopię.

## 2. Pobierzcie pierwszy model

```powershell
ollama pull qwen3.5:4b
ollama list
```

Pobieranie wymaga internetu i wolnego miejsca na dysku. Po pobraniu generowanie odpowiedzi odbywa się lokalnie. `ollama list` pokazuje modele dostępne na danym komputerze; nazwa przekazana do aplikacji musi się z nią zgadzać.

## 3. Uruchomcie aplikację

Z głównego katalogu MealBrAIn:

```powershell
dotnet run --project .\src\MealBrain.ModelLab -- qwen3.5:4b off 0
```

Argumenty po `--` to kolejno:

1. Nazwa modelu: `qwen3.5:4b`.
2. Żądany tryb thinking: `off`, `on` albo `default` (domyślne zachowanie modelu).
3. Temperature: `0`, z kropką przy ułamku, np. `0.5`.

Program ustawia też `num_ctx = 8192`, `num_predict = 2048`, `stream = false` i `keep_alive = 10m`. Czeka na pełną odpowiedź; w tym czasie wyświetla komunikat. Limit jednego żądania wynosi 10 minut. Osiągnięcie limitu tokenów jest sygnalizowane i pozostaje widoczne w logu.

Na początek wpiszcie:

```text
/warmup
```

To osobne żądanie rozgrzewające model. Zapisuje się jako `warmup` i nie wchodzi do historii przepisów. Nie wliczajcie jego czasu do mediany odpowiedzi na przypadki. Jeżeli badacie zimny start, zapiszcie jego czas osobno. Kolejne żądanie wykonajcie po rozgrzewce, gdy model jest jeszcze załadowany.

## 4. Wykonajcie pierwszy przypadek

W działającym programie, przy `Ty>`, wpiszcie:

```text
/case MB-001
```

Program sam:

- rozpocznie nową rozmowę z instrukcją systemową;
- wyciągnie wyłącznie wejście MB-001 z `test-cases.md`;
- pokaże wysyłaną treść i przekaże ją modelowi;
- wyświetli odpowiedź, całkowity czas żądania i czas ładowania raportowany przez Ollamę;
- zapisze żądanie i odpowiedź do pliku.

Otwórzcie [opis MB-001 i pozostałych przypadków](test-cases.md). Sprawdźcie pierwszą odpowiedź według oczekiwanych wyników. Następnie wpiszcie zwykłym tekstem:

```text
Zatwierdzam ten przepis.
```

To kolejna wiadomość tej samej rozmowy. Program wyśle instrukcję, wcześniejsze wiadomości i Waszą nową wypowiedź. Model nie otrzyma odpowiedzi wzorcowej.

Skopiujcie [szablon wyników](results-template.md) pod własną nazwą i uzupełnijcie wiersz MB-001. Pełną nazwę katalogu z logami program wypisuje przy starcie.

## 5. Wykonajcie resztę zestawu

W tej samej aplikacji:

```text
/case MB-002
```

Ta komenda zeruje historię poprzedniego przypadku. Po ocenie pierwszej odpowiedzi wpisujcie kolejno:

```text
Makaronu 180 g, śmietanki 100 ml.
```

Po odpowiedzi modelu:

```text
To dwie porcje. Popraw makaron na 200 g.
```

Sprawdźcie również odpowiedzi pośrednie. Następnie wykonajcie `/case MB-003` aż do `/case MB-006`, używając dalszych wypowiedzi z dokumentu. MB-004 ma tylko pierwsze wejście.

Podczas mierzonego przebiegu korzystajcie z ustalonych wiadomości. Dodatkowe podpowiedzi mogą poprawić wynik; takie rozmowy zapiszcie jako osobny eksperyment. Jeśli model nie dopyta poprawnie, odnotujcie błąd i mimo tego podajcie zaplanowane uzupełnienie, aby zbadać pozostałe zachowania.

## 6. Porównajcie inny model

Zakończcie program:

```text
/exit
```

W PowerShellu:

```powershell
ollama pull gemma4:e2b
dotnet run --project .\src\MealBrain.ModelLab -- gemma4:e2b off 0
```

Wykonajcie `/warmup`, potem identyczne sześć przypadków i uzupełnień. Użyjcie nowej kopii raportu. Nie poprawiajcie promptu między modelami w tym samym porównaniu.

Większy kandydat, przede wszystkim do sprawdzenia na komputerze Patrycji:

```powershell
ollama pull qwen3.5:9b
dotnet run --project .\src\MealBrain.ModelLab -- qwen3.5:9b off 0
```

Jeśli model lub wersja Ollamy odrzuca `think: false`, sprawdźcie obsługiwane ustawienia przez `/api/show` i wersję Ollamy. `default` pomija pole `think` w żądaniu. Taki przebieg oznaczcie osobno — nie deklarujcie wtedy, że rozumowanie było wyłączone. Program nie zmienia tego parametru po cichu.

## Gdzie są instrukcje i wyniki

| Plik | Do czego służy |
|---|---|
| `src/MealBrain.ModelLab/system-prompt.txt` | Wspólna instrukcja wysyłana jako wiadomość `system`. Tu eksperymentujecie z promptingiem. Po zmianie uruchomcie program ponownie zwykłym `dotnet run`. |
| `docs/stage-02/test-cases.md` | Źródło wejść i ręcznych odpowiedzi wzorcowych. Tylko wyodrębnione bloki wejściowe trafiają do API. |
| `runs/<identyfikator>/metadata.json` | Ustawienia, rzeczywista treść instrukcji, hashe instrukcji i danych, wersja Ollamy i lista modeli z digestami/kwantyzacją. Sprzęt dopiszcie w raporcie. |
| `runs/<identyfikator>/turns.jsonl` | Każda linia to osobny zapis JSON: przypadek, identyfikator rozmowy, żądanie z historią, odpowiedź i czas albo błąd. Rozgrzewka jest wyraźnie oznaczona. |

Katalogi `runs`, `bin` i `obj` są ignorowane przez Git. Jeśli chcecie zachować wybrany przebieg w repozytorium, przejrzyjcie logi i skopiujcie potrzebne pliki do np. `docs/stage-02/reports/`. Raport powinien zawierać wyniki, a nie tylko link do ignorowanego pliku na jednym komputerze.

## Jak działa kod C#

Najważniejsze miejsce w `Program.cs` to złożenie żądania i jego wysłanie:

```csharp
var request = new { model, messages, stream = false, think, options, keep_alive = "10m" };
using var response = await http.PostAsJsonAsync("/api/chat", request, serializer);
```

`messages` zawiera rolę `system`, kolejne wiadomości `user` i końcowe odpowiedzi `assistant`. Ollama nie przechowuje za Was historii tego wywołania — program dostarcza ją ponownie w każdym żądaniu. Treść `thinking` pozostaje w surowym logu, jeśli wystąpi; nie jest dodawana do historii jako końcowa odpowiedź.

To ten sam mechanizm HTTP co w laboratorium 03. Dodaliśmy instrukcję systemową, wiele tur rozmowy, wybór przypadków, reset historii i logi. W następnym kroku można zautomatyzować podawanie dalszych wypowiedzi oraz dodać JSON Schema i sprawdzanie pól. Ręczne ocenianie sześciu przypadków pozwoli najpierw zobaczyć, co rzeczywiście należy automatyzować.

## Dodatkowe komendy

- `/new` — nowa swobodna rozmowa z pustą historią i tą samą instrukcją.
- `/paste` — wklejenie własnej wiadomości wielowierszowej; zakończcie osobną linią `/send`.
- `/case MB-002` wpisane ponownie — nowe wykonanie tego przypadku, z nowym identyfikatorem rozmowy.

`ollama run <model>` nadaje się do sprawdzenia, czy model w ogóle odpowiada. Porównanie projektu wykonujcie przez przygotowaną aplikację: ma ustalone instrukcje, parametry, historię i zapis pomiarów.

Źródła techniczne: [API czatu Ollamy](https://docs.ollama.com/api/chat), [sterowanie thinking](https://docs.ollama.com/capabilities/thinking), [metadane modeli](https://docs.ollama.com/api/tags).
