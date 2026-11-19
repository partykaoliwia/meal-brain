# Próba instrukcji v2 — Qwen 3.5 4B

Data: 4 listopada 2026. Źródło: `runs/20261104-150924-a1d00587971a4a218fcd575a3e897ce3`. Oceniono MB-001, MB-003 i MB-004, każdy po jednym wykonaniu. Nie jest to wynik całego zestawu sześciu przypadków.

Aktualizacja: wykonano również [MB-002, MB-005 i MB-006 w drugiej sesji](remaining-cases/report.md), a następnie [standardowy MB-002 z częściowym uzupełnieniem i poprawką](standard-mb002/report.md). MB-002 zaliczony; MB-005 nadal błędnie interpretuje konflikt, a MB-006 fałszywie potwierdził zatwierdzenie bez zgody.

## Podsumowanie sześciu ustalonych scenariuszy

| Przypadek | Wynik v2 | Główna obserwacja |
|---|---|---|
| MB-001 | Zaliczony | Poprawny kompletny import i zatwierdzenie. |
| MB-002 | Zaliczony | Poprawne częściowe uzupełnienie, korekta 180 g → 200 g i zachowanie pozostałych danych. |
| MB-003 | Niezaliczony | Wskazuje brak czasu całkowitego, lecz zamiast o niego zapytać prosi o zatwierdzenie. |
| MB-004 | Niezaliczony | Dopisuje nieistniejące braki i konflikt. |
| MB-005 | Niezaliczony | Błędnie interpretuje konflikt jako łączną masę pomidorów i mozzarelli. |
| MB-006 | Niezaliczony, błąd krytyczny | Potwierdza zatwierdzenie bez zgody; w dodatkowej rozmowie błędnie oznacza porcje jako niepodane. |

**Łącznie v2: 2/6 w pełni zaliczonych przypadków; v1: 0/6.** To mały zestaw roboczy, a nie estymacja ogólnej skuteczności modelu. Wynik pochodzi z trzech sesji o tej samej konfiguracji. Dla MB-002 uwzględniamy ustaloną rozmowę ze standard-mb002; wcześniejszego wariantu z jednorazowym uzupełnieniem nie liczymy jako dodatkowego zaliczonego przypadku. Dalsze sekcje opisują pierwotną próbę trzech przypadków.

Dowody: [metadane](metadata.json), [pełne żądania i odpowiedzi](turns.jsonl), [archiwalna instrukcja v2](../../prompts/system-prompt-v2.txt). Porównanie: [raport v1](../2026-11-03-qwen3.5-4b-v1/report.md). Kryteria: [przypadki testowe](../../test-cases.md).

## Zgodność warunków porównania

Na podstawie metadanych obu przebiegów potwierdzono:

- Ten sam model `qwen3.5:4b` i digest `d8b0f5e9760cd1682034f292d7ef72ec46f432149be0df7574bf2d6e92e38c04`.
- Ten sam komputer `OLIWKA`, Ollama `0.40.13` i runtime `.NET 10.0.14`.
- Te same ustawienia: temperatura 0, `think: false`, kontekst 8192, limit odpowiedzi 2048.
- Ten sam hash zbioru przypadków i identyczne teksty pierwszych wejść w porównywanych trzech przypadkach.
- Instrukcja v2 ma hash `0E95DE93D1EA088F4FE4DF987221B9C43E753BBED71400D9246DE11E262D819C`, zgodny z przygotowanym wariantem.

Zapis obejmuje rozgrzewkę i siedem odpowiedzi czatu. Wszystkie mają `done_reason: stop`; nie ma zapisanych błędów żądań. Użytkowniczka dodatkowo zatwierdziła MB-003 i MB-004. Te dodatkowe wiadomości opisujemy jako obserwacje; nie rozszerzamy nimi wcześniejszych kryteriów ani porównania czasów.

## Ocena jakości

| Przypadek | Poprawa względem v1 | Pozostałe problemy | Zaliczenie według pełnych kryteriów |
|---|---|---|---|
| MB-001 | Wszystkie składniki, ilości, kroki, porcje i czas prawidłowe. Zniknęły sztuczne braki. Prosi o zatwierdzenie, a po zgodzie odpowiada tylko „Przepis zatwierdzony”. | Nie stwierdzono problemu naruszającego kryteria tego przypadku. | Tak |
| MB-003 | Poprawnie rozpoznaje 2 porcje i brak czasu całkowitego, nie pyta o sól. Po uzupełnieniu przyjmuje 30 minut i zachowuje 20 minut gotowania w kroku. | Pierwsza odpowiedź kończy się „Czy zatwierdzasz?” zamiast pytaniem o brakujący czas. Samo wymienienie braku nie realizuje wymaganego dopytania. Osobny czas gotowania nie trafia do podsumowania, choć pozostaje poprawny w krokach. | Nie — właściwe pytanie nie zostało zadane |
| MB-004 | Poprawne 2 porcje i 20 minut, zachowane zakresy, ułamki, ilość opisowa i opcjonalny miód. Zniknęły pytania o rozmiar porcji. | W sekcji braków nadal wymienia brak osobnego czasu smażenia, niedorzeczny brak masy po odsączeniu oleju i rzekomy konflikt mąki, który sam opisuje jako spójny. | Nie — sekcja braków zawiera nieistniejące problemy |

**Wynik: 1/3 w pełni zaliczonych przypadków**, wobec 0/3 dla tych samych przypadków w v1. Jest to ocena trzech przypadków roboczych, bez powtórzeń; nie podajemy na tej podstawie ogólnej skuteczności modelu.

W MB-003 model nie twierdzi wprost, że dane są kompletne przed uzupełnieniem: wskazuje brak czasu, lecz wybiera niewłaściwe pytanie. Oceniany błąd dotyczy prowadzenia rozmowy. W MB-004 użytkowniczka zatwierdziła przepis mimo błędnych uwag modelu. Późniejsze krótkie potwierdzenie nie usuwa błędów pierwszej odpowiedzi.

## Czas pierwszej odpowiedzi

Porównujemy wyłącznie te same trzy przypadki, po jednym wykonaniu, bez rozgrzewki i dalszych tur:

| Przypadek | v1 [s] | v2 [s] | Tokeny odpowiedzi v1 → v2 |
|---|---:|---:|---|
| MB-001 | 65,113 | 28,110 | 553 → 272 |
| MB-003 | 52,340 | 22,239 | 449 → 204 |
| MB-004 | 77,268 | 32,631 | 695 → 353 |
| **Mediana** | **65,113** | **28,110** | — |

Mediana zmalała o około 57%. Odpowiedzi są również krótsze: łącznie 1697 → 829 tokenów w tych trzech turach. Skrócenie generowanego tekstu jest zgodne z kierunkiem zmiany czasów, ale pojedyncze pomiary nie izolują wpływu obciążenia komputera ani innych czynników wykonania.

Potwierdzenie zatwierdzenia w MB-001: 39,237 s i 294 tokeny w v1 → 3,866 s i 8 tokenów w v2. Dodatkowe potwierdzenia MB-003 i MB-004 zajęły odpowiednio 3,189 s i 4,570 s; nie porównujemy ich z v1, gdzie takich tur nie wykonano.

## Wniosek i następny krok

Instrukcja v2 poprawiła odczyt porcji, ograniczyła część zbędnych pytań i skróciła odpowiedzi. Nie rozwiązała jeszcze w pełni wykrywania braków ani wyboru właściwego pytania.

Uzupełnienie pokrycia o pozostałe przypadki i wykonanie standardowego MB-002 zakończono. Bieżące zadanie to porównanie drugiego modelu na tej samej instrukcji. Należy zachować trzy sesje i odróżniać ustalone scenariusze od dodatkowych rozmów diagnostycznych.

Po wykonaniu pozostałych przypadków porównać drugi model z tą samą instrukcją v2. Pozwoli to oddzielić praktyczny wpływ wyboru modelu od wpływu promptu, zanim zaczniemy kolejną iterację instrukcji. Dalsze dopracowanie powinno dotyczyć szczególnie rozróżnienia „wykryto brak → pytanie o dane” i „komplet danych → zatwierdzenie”, bez wpisywania odpowiedzi z konkretnych przepisów do promptu.
