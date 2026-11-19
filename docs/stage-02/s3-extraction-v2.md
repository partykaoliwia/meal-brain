# S3 — sprawdzenie poprawionej instrukcji ekstrakcji

**Próba zakończona:** [raport dwóch odczytów](reports/2026-11-04-s3-extraction-v2/report.md) potwierdza poprawne zachowanie konfliktu i czasu tostowania. Późniejszy zestaw dał 2/6, a [regresja z instrukcją v3 — 5/6](reports/2026-11-04-s3-extraction-check-v2/report.md). Serię zamknięto na etap 2; aktualne materiały to [podsumowanie S3](s3-stage-02-summary.md) i [przekazanie zespołowi](team-handoff.md). Poniżej zachowano protokół wcześniejszej próby. Domyślny tryb interaktywny używa instrukcji v3; dawny zestaw v1 nadal wybiera v2 do odtworzenia wyników. Walidator pozostaje v3.

Po [pierwszej próbie trzech importów](reports/2026-11-04-s3-extraction-v1/report.md) aktywny tryb `--extract` używa wariantu **`s3-extraction-v2`**. Kontrakt i JSON Schema pozostają w wersji 1. Dawny prompt v2 rozmów porównawczych to inny plik; jego wyników tu nie zmieniamy.

Zmiany przed opisaną próbą: instrukcja daje przykład konfliktu, wymaga zapisywania jawnych czasów etapów także w `times` i doprecyzowuje zapis „maksymalnie”. Kod rozpoznaje `plaster`, `plastry`, `plastrów`. Nadal odrzuca zakres przy nierozstrzygniętym konflikcie. Nie naprawia automatycznie odpowiedzi modelu. W chwili ustalania poniższego protokołu skuteczność nowej instrukcji nie była jeszcze zmierzona.

## Co uruchomić teraz

W katalogu MealBrAIn:

```powershell
dotnet run --project .\src\MealBrain.ModelLab -- --extract gemma4:12b
```

Na początku powinno być `s3-extraction-v2`. Wykonaj tylko dwa odczyty; po każdym poczekaj na pełny wynik.

Najpierw:

```text
/case MB-005
```

Potem wklej jako jedną wiadomość dokładnie ten sam opis tostów:

```text
Przepis na tosty z serem. Dwie kromki chleba tostowego. Dwa plastry sera gouda. Trochę ketchupu. Położyć ser na chlebie, wsadzić do tostera, tostować 2 minuty. Jeść z ketchupem. Całkowity czas przygotowania to maksymalnie 10 minut.
```

Zakończ `/exit` i zachowaj ścieżkę. Nie podawaj tu jeszcze „200 g pomidorów” ani „zatwierdzam”: każda wiadomość nadal oznacza nowy import. Odczyty mogą trwać kilka minut; zmiany nie miały na celu przyspieszenia modelu.

## Oczekiwania ustalone przed ponowną próbą

| Wejście | Warunki zaliczenia odczytu | Oczekiwany wynik kontroli C# |
|---|---|---|
| MB-005 | 2 porcje, 15 min; pięć składników z czosnkiem 5 g; pomidory `unknown` z jednostką g; jeden konflikt z 150 g i 200 g oraz dosłownymi cytatami; brak wymyślonego zakresu; poprawne kroki i istniejące identyfikatory | `needs_clarification`, bez błędów i uwag, tylko rozstrzygnięcie konfliktu |
| Tosty | Tytuł, `servings=null`, trzy składniki: 2 kromki chleba, 2 plastry goudy, ketchup `text: trochę` bez wymyślonej ilości; `total=0–10`; **`cooking=2–2`**; przygotowanie i odpoczynek `null`; wszystkie czynności, czas i podanie z ketchupem pozostają w krokach; brak konfliktów i składników dopisanych z wiedzy | `needs_clarification`, bez błędów; brak porcji i uwaga o opisowej ilości ketchupu; brak uwagi o jednostce plastrów |

W tostach nazwa jednostki może być `plaster`, `plastry` albo `plastrów`, bez zmiany liczby 2. Wielkość liter w „Trochę” nie ma znaczenia dla oceny treści. Każdy cytat ma występować dosłownie w wejściu. `ingredientIds` muszą wskazywać istniejące składniki.

Nie oczekujemy `ready_for_review`: oba źródła celowo pozostają niekompletne. Sprawdzamy poprawne dane i właściwe blokady. Sam brak `invalid_output` nie wystarczy do zaliczenia — ręcznie sprawdzamy też pominięcia, zwłaszcza `times.cooking`.

## Co po tej próbie

Jeżeli oba odczyty spełnią oczekiwania, zamrozimy instrukcję v2 i sprawdzimy szerszy zakres: kompletny przepis, pozostałe braki, prawdziwy zakres, materiał bez przepisu i polecenie ukryte w źródle. Nie wracamy od razu do pobierania kolejnych modeli.

Jeśli konflikt ponownie da `invalid_output`, zachowujemy blokadę i zapisujemy, że sama instrukcja nie wystarcza. Wtedy dobieramy jawny mechanizm ponowienia albo ręcznej poprawy z zachowaniem surowego wyniku; nie obniżamy reguł walidacji i nie powtarzamy prób aż trafi się dobra odpowiedź.

Maria może już równolegle przygotowywać moduł uzupełniania na wspólnym kontrakcie: otrzymuje szkic i wynik walidacji, zmienia wskazane pola, ponownie waliduje i pilnuje zgody na konkretną wersję. `invalid_output` wymaga obsługi błędu odczytu, a nie zwykłego pytania o brakujące porcje. Patrycja może mapować S1 do tego samego kontraktu. Żadne z tych zadań nie wymaga ukończenia bieżącej regresji Gemmy.

Do demo etapu 2 nadal potrzebujemy całego przepływu: odczyt → uzupełnienie/rozstrzygnięcie → przegląd → jawna zgoda. Ten tryb realizuje pierwszy krok i kontrolę danych.
