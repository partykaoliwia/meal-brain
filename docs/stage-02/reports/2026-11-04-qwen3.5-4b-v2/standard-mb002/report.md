# Standardowy MB-002 — częściowe uzupełnienie i korekta

Data: 4 listopada 2026. Źródło: `runs/20261104-152931-681a80af602f48fc921542c3ff175484`. Dowody: [metadane](metadata.json), [rozmowa](turns.jsonl). Podsumowanie całego zestawu: [raport v2](../report.md).

Model `qwen3.5:4b`, temperatura 0, `think: false`, kontekst 8192, limit odpowiedzi 2048, instrukcja v2 i dane testowe zgodne z wcześniejszymi sesjami. Hash instrukcji: `0E95DE93D1EA088F4FE4DF987221B9C43E753BBED71400D9246DE11E262D819C`.

## Ocena

| Tura | Zaobserwowane zachowanie | Wynik |
|---|---|---|
| Pierwsze wejście | Poprawne składniki, kroki i czas 20 min. Wskazuje wszystkie trzy braki: porcje, ilość makaronu i śmietanki. Prosi o uzupełnienie bez zbędnych pytań. | Poprawne |
| „Makaronu 180 g, śmietanki 100 ml.” | Wpisuje 180 g i 100 ml, zachowuje resztę danych. Nadal oznacza porcje jako nieznane i pyta wyłącznie o nie. | Poprawne |
| „To dwie porcje. Popraw makaron na 200 g.” | Ustala 2 porcje, zastępuje 180 g makaronu wartością 200 g. Zachowuje 200 g szpinaku, 100 ml śmietanki, 1 łyżkę oliwy, sól do smaku, czas i kroki. Przedstawia przepis do zatwierdzenia. | Poprawne |
| Dodatkowe „Tak” | Rozpoznaje zgodę po własnym pytaniu o zatwierdzenie; odpowiada „Przepis zatwierdzony”. | Poprawne; dodatkowa obserwacja |

**Standardowy MB-002 zaliczony: 5/5 wymiarów, bez zaobserwowanych błędów krytycznych.** Wykonano jeden pełny przebieg ustalonej rozmowy; wcześniejszy wariant z jednorazowym uzupełnieniem pozostaje osobnym scenariuszem. Dodatkowe zatwierdzenie nie jest potrzebne do przyznania 5/5 według pierwotnych kryteriów MB-002.

## Czasy

| Tura | Czas [s] |
|---|---:|
| Pierwsze wejście | 20,737 |
| Częściowe uzupełnienie | 15,253 |
| Korekta i porcje | 14,836 |
| Dodatkowe zatwierdzenie | 2,780 |

Rozgrzewka: 0,471 s. Wszystkie odpowiedzi mają `done_reason: stop`; brak błędów żądań. Czasy dotyczą tego pojedynczego przebiegu.

## Następny krok

Ustalony zestaw sześciu scenariuszy ma już wykonanie dla instrukcji v2. Wynik łączny: 2/6 całkowicie zaliczonych przypadków, z krytycznym błędem zatwierdzenia bez zgody w MB-006. Można porównać drugi model z tą samą instrukcją i ustalonymi wiadomościami, bez kolejnej zmiany promptu.
