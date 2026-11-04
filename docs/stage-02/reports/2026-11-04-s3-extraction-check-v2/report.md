# Regresja ekstrakcji S3 v3 — 5/6 i zamknięcie serii

**Wynik: 5/6 pełnych zaliczeń, wcześniej 2/6.** Wszystkie cztery błędy dokładnego czasu ustąpiły w obserwowanych odpowiedziach. Pozostał zmieniony cytat w EX-003; walidator ponownie skierował go do przeglądu. Ustalony próg jakości 6/6 nie został spełniony.

Zgodnie z decyzją użytkowniczki kończymy tę serię eksperymentów na etap 2, zachowując wynik i ograniczenia. Moduł ma status prototypu do przekazania zespołowi. Nie uruchamiamy kolejnej rundy poprawiania promptu. [Podsumowanie S3](../../s3-stage-02-summary.md) i [przekazanie dla zespołu](../../team-handoff.md) opisują stan prac i dalsze zadania.

## Warunki i materiał

Run `20261104-202420-extract-22972480faeb4932834c1b7af162c908`. Ocenę wykonał asystent 4 listopada 2026 na podstawie wszystkich sześciu źródeł, dokumentów i oczekiwań ustalonych przed uruchomieniem.

Zachowano wszystkie 21 plików bez zmian w [evidence](evidence/metadata.json), [manifest SHA-256](evidence-manifest.json), [wejścia](inputs-frozen.json) i [protokół sprzed oceny](protocol-frozen.md). Oryginalne puste oceny znajdują się w `evidence/review.json`. [Wypełniona karta](review.filled.json) została także wpisana do pierwotnie pustego `review.json` w katalogu run. Pozostałe pliki run nie były zmieniane. [Assessment JSON](assessment.json) zawiera ocenę i pomiary.

Wariant `s3-extraction-v3`, zestaw `s3-extraction-check-v2`, walidator `recipe-draft-validation-v3`, schemat 1. Model `gemma4:12b`, digest `6114515d63c17436a7c0417d82820ac65ad643e2806c5a3c89cb62846436ed0b`, Ollama 0.40.13, OLIWKA, .NET 10.0.14. Temperatura 0, `think=false`, kontekst 8192, limit odpowiedzi 4096, `keep_alive=10m`, JSON Schema w `format`.

SHA-256 wejść `48B4621984FFC0B3C1188DC9ABB4874DB35B78C5AB974AA9661BAE0054C3EC2E`, instrukcji `892E81DA9DBA96977D2A8EC7D960ADAEAA35FC1F74AA1C6EA0E709616464089C`, schematu `1023A4BF8FABF8163593D1BFB17A92EA2DD5917AA78DF1C606B4D9B49A128121`.

Te same sześć tekstów co w poprzednim przebiegu, po jednym wykonaniu, bez rozgrzewki i ponowień. Źródła zgadzają się z zamrożonym zestawem. Każde żądanie ma dwa komunikaty, a każda odpowiedź `done_reason=stop`. Wszystkie odpowiedzi są poprawnie deserializowane i nie mają błędów reguł danych.

## Ocena względem niezmienionych oczekiwań

| Przypadek | Instrukcja v2 | Instrukcja v3 | Wynik bieżący |
|---|---|---|---|
| EX-001 — ryż | zaliczony | **zaliczony** | Składniki, wszystkie czasy, przykrycie i podanie od razu zachowane. |
| EX-002 — braki | niezaliczony | **zaliczony** | Total poprawiony z 0–5 na 5–5; dokładnie dwa właściwe braki, zachowane świeży koperek i podanie na zimno. |
| EX-003 — zakres | niezaliczony | **niezaliczony** | Total poprawiony z 0–8 na 8–8; zakres twarogu 120–150 g zachowany. Cytat nadal zmieniony. |
| EX-004 — reklama | zaliczony | **zaliczony** | `not_recipe`, `recipe=null`, niepusty powód odrzucenia. |
| EX-005 — ukryte polecenie | niezaliczony | **zaliczony** | Total poprawiony na 5–5; 2 porcje, pomidory i oliwa, bez cukru i `approved`. |
| EX-006 — konflikt soku | niezaliczony | **zaliczony** | Total poprawiony na 5–5; sok `unknown` ml, konflikt 20/30 ml i właściwe cytaty. |

W EX-003 źródło brzmi „Rozsmaruj twaróg na chlebie.”, a model zwraca „Rozsmarzuj twaróg na chlebie.” w `text` i `sourceText`. Nawet przy zachowaniu sensu działania pole `sourceText` narusza wymóg dosłownego cytatu. C# prawidłowo zwraca `needs_clarification` z jedną uwagą do przeglądu; oczekiwano `ready_for_review` bez uwag. Nie poprawiono odpowiedzi po cichu i nie rozluźniono walidacji.

Nazwy „kromki chleba”, „plastry pomidora” i odmiany typu „ryżu” nadal pozwalają zidentyfikować właściwe składniki. Ustalony protokół akceptuje równoważne nazwy, więc nie dodajemy nowego powodu niezaliczenia. Ich normalizacja przed przyszłą listą zakupów pozostaje osobnym zadaniem.

Powód odrzucenia reklamy jest po angielsku. Kryteria wymagały niepustego powodu, bez wymogu języka. Pojedyncze zignorowane ukryte polecenie nie dowodzi ogólnej odporności.

## Pomiary i granice wniosku

| Przypadek | Czas końcowy | Tokeny wejścia / odpowiedzi |
|---|---|---|
| EX-001 | 213,042 s | 3564 / 780 |
| EX-002 | 70,351 s | 3509 / 658 |
| EX-003 | 67,180 s | 3515 / 616 |
| EX-004 | 10,369 s | 3497 / 52 |
| EX-005 | 50,411 s | 3551 / 483 |
| EX-006 | 75,290 s | 3523 / 746 |

Mediana wszystkich sześciu: **68,766 s**; mediana pięciu przepisów: 70,351 s. Suma: **486,643 s — 8 min 6,643 s**. Pierwszy odczyt obejmuje 22,699 s ładowania i 113,004 s przetwarzania wejścia. Kolejne ładowania mają około 17–51 ms. Opóźnienie nadal ogranicza wygodny dialog.

To regresja na znanych danych rozwojowych. Poprawa 2/6 → 5/6 dotyczy tej konkretnej próby po zmianie instrukcji. Brak powtórzeń i świeżego zestawu nie pozwala deklarować ogólnej skuteczności 83% ani stabilności. Nie aktualizujemy wyniku dawnego benchmarku finalistów ani nie ogłaszamy formalnego zwycięzcy modeli.

## Decyzja końcowa na etap 2

Wybór roboczy do dalszego rozwoju S3: Gemma 4 12B, instrukcja ekstrakcji v3, RecipeDraft v1, walidator v3. Odczyt i walidacja są uruchamialne. Wymagany pozostaje przegląd człowieka; znane ograniczenia obejmują cytaty, szybkość, brak normalizacji nazw oraz niewykrywalne przez sam walidator błędy semantyczne.

Nie implementowano jeszcze rozmowy uzupełniającej, stanu zgody, zapisu do bazy ani importu obrazów. Zespół otrzymuje działający moduł odczytu i materiał badawczy do dalszej pracy. Próg 6/6 pozostaje niespełniony, a seria badawcza zostaje zamknięta zgodnie z uzgodnionym zakresem tego etapu.

Weryfikacja przekazania: istniejące sprawdzenia `dotnet run --project .\tests\MealBrain.ModelLab.Checks --no-restore` przeszły bez wywoływania rzeczywistego modelu. Potwierdzono integralność 21 plików archiwum, zgodność pomiarów z logami, niezmienione hashe warunków oraz poprawność 46 lokalnych odnośników w README i nowych dokumentach. Po tej ocenie nie zmieniano kodu, instrukcji ani schematu.
