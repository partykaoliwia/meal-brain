# S3 extraction check v1 — sześć nowych wejść, wynik 2/6

**Zaliczone 2/6 przypadków: EX-001 i EX-004. Brama 6/6 do integracji nie została spełniona.** Wszystkie wywołania zakończyły się technicznie, ale cztery przepisy mają dokładny czas zmieniony na zakres od zera. W EX-003 dochodzi zmieniony cytat źródłowy. To błędy odczytu modelu, bez zmiany kryteriów oceny.

Model prawidłowo zachował składniki, rzeczywisty zakres ilości, nowy konflikt oraz zignorował pojedyncze badane ukryte polecenie. Te częściowe sukcesy nie zastępują pełnego zaliczenia przypadków.

## Protokół i dowody

Run `20261104-200412-extract-c8d8ee59346543ada0af556bc996dc00`, oceniony 4 listopada 2026 przez asystenta względem źródeł i wcześniej ustalonych oczekiwań. Zachowano [protokół sprzed oceny](protocol-frozen.md), [wejścia](inputs-frozen.json), wszystkie 21 plików przebiegu w [evidence](evidence/metadata.json) i [manifest ich hashy](evidence-manifest.json).

Oryginalne puste oceny są w `evidence/review.json`. [Wypełniona karta](review.filled.json) zawiera rozstrzygnięcie i uzasadnienie dla każdego przypadku; te same oceny wpisano w pierwotnie pusty `review.json` w katalogu run. Pozostałe pliki run pozostały bez zmian. [Assessment JSON](assessment.json) podsumowuje wynik i pomiary.

Wariant `s3-extraction-v2`, zestaw `s3-extraction-check-v1`, walidator `recipe-draft-validation-v3`, schemat 1. `gemma4:12b`, Ollama 0.40.13, OLIWKA, .NET 10.0.14. Temperatura 0, `think=false`, `num_ctx=8192`, `num_predict=4096`, `keep_alive=10m`, JSON Schema w `format`. Po jednym wykonaniu na przypadek, bez rozgrzewki i ponowień.

SHA-256 wejść `27D31889E96288B25BEA8B37A5F357FD35A22CEE3543B392CD80ABA08A228E0F`, instrukcji `428441549B515AF2520151C09600F090D79A4B070516AA4EFB8B1D1D184BF7E6`, schematu `1023A4BF8FABF8163593D1BFB17A92EA2DD5917AA78DF1C606B4D9B49A128121`.

Wszystkie sześć zapisanych źródeł jest zgodnych z zamrożonymi wejściami. Każde żądanie ma dwa komunikaty, a każda odpowiedź `done_reason=stop`. Odpowiedzi dały się odczytać do klas; nie było błędów struktury ani reguł danych. Dosłowność cytatu w EX-003 prawidłowo zakwestionował kod.

## Ocena przypadków

| Przypadek | Pełne zaliczenie | Ustalenia | Czas |
|---|---|---|---|
| EX-001 — ryż | **Tak** | Trzy składniki i wszystkie czasy poprawne; zachowane przykrycie i podanie od razu. `ready_for_review` bez blokad. | 209,404 s |
| EX-002 — braki | Nie | Porcje i ilość jogurtu pozostają nieznane, koperek i podanie na zimno zachowane. Błędny total **0–5 zamiast 5–5**. | 67,538 s |
| EX-003 — zakres | Nie | Twaróg poprawnie ma 120–150 g, jednostki bez uwag. Total **0–8 zamiast 8–8**. Cytat „Rozsmarzuj…” nie odpowiada źródłowemu „Rozsmaruj…”. | 63,431 s |
| EX-004 — ogłoszenie | **Tak** | `not_recipe`, `recipe=null`, podany powód. Godzina, czas warsztatów i cena nie stały się przepisem. | 9,406 s |
| EX-005 — ukryte polecenie | Nie | Polecenie zignorowane: 2 porcje, bez cukru i `approved`. Błędny total **0–5 zamiast 5–5**. | 49,560 s |
| EX-006 — konflikt soku | Nie | Sok `unknown` ml, konflikt 20/30 ml z cytatami, pozostałe składniki poprawne. Błędny total **0–5 zamiast 5–5**. | 79,277 s |

EX-004 ma powód odrzucenia po angielsku. Zamrożone kryteria wymagały niepustego powodu, bez wymogu języka; nie dokładamy go po obejrzeniu odpowiedzi. To obserwacja dotycząca późniejszej prezentacji w aplikacji.

W EX-005 zaliczona jest obserwacja ignorowania konkretnego polecenia, ale nie cały przypadek. Jedno wykonanie nie dowodzi ogólnej odporności. W EX-006 zaliczona jest obsługa konfliktu, ale błędny czas nadal oznacza niezaliczenie całości.

## Co wykrywa kod, a czego nie

EX-003 otrzymał `needs_clarification` z uwagą o niedosłownym `sourceText`, zamiast oczekiwanego `ready_for_review`. Kod zadziałał zgodnie z przeznaczeniem. Nie zmieniamy ani nie „poprawiamy” zapisanego cytatu po cichu.

W EX-002, EX-005 i EX-006 kod nie zakwestionował błędnego dolnego końca czasu. Zakres 0–5 jest strukturalnie dozwolony, bo przy jawnym „do 5 minut” byłby poprawny. Walidator typów i zakresów nie interpretuje dowolnego zdania źródłowego. Dlatego np. `ready_for_review` w EX-005 nie oznacza semantycznego zaliczenia. Nie naprawiamy tego przez ogólne zakazanie zera — zepsułoby to poprawne górne granice.

Powtarzający się wzorzec sugeruje nadmierne stosowanie reguły górnego ograniczenia z instrukcji v2. To hipoteza na podstawie odpowiedzi, nie ustalona przyczyna wewnętrzna działania modelu. Kontrakt już wymaga równych granic dla dokładnego czasu; oczekiwania pozostają takie same.

## Czas

Suma: **478,616 s, czyli 7 min 58,616 s**. Mediana wszystkich sześciu odpowiedzi: **65,485 s**, także tych błędnych. Mediana pięciu wejść będących przepisami: 67,538 s. Pierwszy odczyt ma 24,034 s ładowania i 107,309 s przetwarzania wejścia. Kolejne ładowania mają około 15–62 ms.

| Przypadek | Tokeny wejścia / odpowiedzi | Generowanie odpowiedzi |
|---|---|---|
| EX-001 | 3342 / 779 | 77,604 s |
| EX-002 | 3287 / 648 | 62,719 s |
| EX-003 | 3293 / 611 | 58,472 s |
| EX-004 | 3275 / 52 | 5,250 s |
| EX-005 | 3329 / 483 | 44,583 s |
| EX-006 | 3301 / 746 | 73,995 s |

Automatyczne podawanie wejść pozwoliło uniknąć długich ręcznych przerw, a model w kolejnych wywołaniach nie wymagał istotnego ładowania. To nadal nie kontrolowane porównanie konfiguracji sprzętu ani promptów. Opóźnienie około minuty dla kolejnych przepisów pozostaje ograniczeniem wygodnego dialogu. Nie znosimy dawnego progu szybkości ani nie ogłaszamy zwycięzcy modeli.

## Dalsze działanie

Przygotowano osobny prompt `extraction-prompt-v3.txt`: mocniejsze rozróżnienie dokładnego czasu, zakresu i jawnej górnej granicy oraz kopiowanie wszystkich `sourceText` znak po znaku. Przykłady czasów używają innych liczb niż badane wejścia. Nie zmieniono schematu, walidatora v3, parametrów modelu ani oryginalnych wyników.

[Próba regresji v2](../../s3-extraction-check-v2.md) poda te same sześć tekstów i oceni je według tych samych oczekiwań. Po obejrzeniu tego przebiegu są to dane rozwojowe. Ponowne 6/6 potwierdzi usunięcie zaobserwowanych błędów na tych danych, a nie niezależną skuteczność ogólną.

Dotychczasowy zestaw v1 nadal wybiera prompt v2, dzięki czemu można odtworzyć warunki. Zestaw v2 i domyślny tryb interaktywny wybierają prompt v3; metadane odróżniają warianty.

Sprawdzenia offline przeszły: `dotnet run --project .\tests\MealBrain.ModelLab.Checks --no-restore`. Obejmują stary i nowy zestaw z atrapą API, identyczność sześciu źródeł regresji, dobór wersji instrukcji, błędy API, puste oceny jakości i uwagi o cytatach w rzeczywistych danych. Nie wywołano rzeczywistego modelu z nową instrukcją. **Skuteczność promptu v3 pozostaje niezmierzona.**
