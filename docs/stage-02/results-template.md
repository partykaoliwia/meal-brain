# Etap 2 — szablon wyników sześciu przypadków roboczych

Skopiujcie ten plik dla konkretnego modelu i konfiguracji. Zachowajcie pusty szablon. Opisy wejść i poprawnych wyników znajdują się w [zestawie przypadków](test-cases.md).

**Status szablonu: żaden test nie został tu jeszcze wykonany.** Kreska oznacza brak pomiaru, nie wynik 0.

## Konfiguracja

| Parametr | Wartość |
|---|---|
| Data i identyfikator przebiegu | — |
| Osoba wykonująca / oceniająca | — |
| Komputer: CPU, GPU, VRAM, RAM | — |
| Wersja Ollamy | — |
| Pełna nazwa modelu, ID/digest i kwantyzacja | — |
| Wersja instrukcji agenta / commit | — |
| Wersja danych testowych / commit | — |
| Format odpowiedzi i wersja schematu, jeśli używany | — |
| Temperature | — |
| Thinking: jawne ustawienie lub domyślne, jeśli nieustawione | — |
| Kontekst, limit odpowiedzi, seed i inne ustawienia, jeśli używane | — |
| Rozgrzewka i sposób pomiaru czasu | — |

W każdym przypadku zaczynamy od pustej historii. W dalszej rozmowie tego samego przypadku zachowujemy wszystkie jego wcześniejsze wiadomości. Podane w zestawie uzupełnienia przekazujemy w ustalonej kolejności; zapisujemy też ewentualne dodatkowe wiadomości.

## Ocena jakości

Wpisujcie 0 albo 1 według definicji w zestawie. „Błąd krytyczny” i „Zaliczony” przyjmują tak/nie. Dla MB-004 dalsza rozmowa nie jest wymagana.

| Przypadek | Składniki | Metadane | Kroki | Decyzja i pytania | Dalsza rozmowa | Suma | Błąd krytyczny | Zaliczony |
|---|---|---|---|---|---|---|---|---|
| MB-001 | — | — | — | — | — | — / 5 | — | — |
| MB-002 | — | — | — | — | — | — / 5 | — | — |
| MB-003 | — | — | — | — | — | — / 5 | — | — |
| MB-004 | — | — | — | — | nie dotyczy | — / 4 | — | — |
| MB-005 | — | — | — | — | — | — / 5 | — | — |
| MB-006 | — | — | — | — | — | — / 5 | — | — |
| **Łącznie** | | | | | | **— / 29** | **— przypadków** | **— / 6** |

## Format i czas

Jeśli odpowiedzi są tekstowe, w kolumnach JSON i schematu wpiszcie „nie dotyczy”. Przy wielu turach format musi być poprawny w każdej wymaganej odpowiedzi. Czas pierwszej odpowiedzi to czas od wysłania pierwszego wejścia do otrzymania całej odpowiedzi, bez czasu czytania i pisania przez człowieka. Dla kolejnych tur zachowajcie czasy w zapisie rozmowy. Zimny start raportujcie osobno.

| Przypadek | Parsowanie JSON poprawne | Zgodność ze schematem | Pierwsza odpowiedź [ms] | Plik z pełnym zapisem rozmowy |
|---|---|---|---|---|
| MB-001 | — | — | — | — |
| MB-002 | — | — | — | — |
| MB-003 | — | — | — | — |
| MB-004 | — | — | — | — |
| MB-005 | — | — | — | — |
| MB-006 | — | — | — | — |

- Mediana czasu pierwszej odpowiedzi: —
- Najwolniejsza pierwsza odpowiedź: —
- Czas zimnego startu i przypadek, na którym go zmierzono: —

## Analiza błędów

Dla każdego błędu wpiszcie identyfikator przypadku i turę, krótki fragment odpowiedzi, oczekiwane zachowanie oraz wpływ na użytkowniczkę. Oddzielcie obserwację od przypuszczenia o przyczynie.

| Przypadek i tura | Zaobserwowany błąd / cytat | Co powinno się wydarzyć | Krytyczny? | Hipoteza przyczyny |
|---|---|---|---|---|
| — | — | — | — | — |

## Wnioski po przebiegu

- Które zachowania model wykonał poprawnie? —
- Które błędy utrudniają użycie go w MealBrAIn? —
- Czy czas odpowiedzi jest akceptowalny na tym komputerze? —
- Co zmieniamy w kolejnym eksperymencie i dlaczego? —
- Czy powtórzenie trudnych przypadków dało ten sam wynik? —

Wnioski dotyczą tylko tego zestawu, konfiguracji i sprzętu. Zmiana promptu oznacza nowy przebieg; nie zastępujcie wcześniejszych wyników poprawioną wersją.
