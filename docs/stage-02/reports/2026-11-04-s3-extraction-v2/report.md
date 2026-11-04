# S3 extraction v2 — ponowna próba konfliktu i czasu etapu

**Oba wskazane błędy modelu ustąpiły w tej próbie:** pomidory mają efektywną ilość `unknown` przy konflikcie 150/200 g, a czas tostowania jest zapisany jako `cooking=2–2`. W tostach pozostała niepotrzebna uwaga aplikacji o poprawnej jednostce „kromki”. Poprawiono ją w kodzie, bez kolejnego wywołania modelu.

To dwa znane przypadki rozwojowe, po jednym wykonaniu. Wynik nie potwierdza stabilności ani ogólnej skuteczności modelu. Nie zmienia punktacji dotychczasowych finalistów i nie oznacza ukończenia przepływu rozmowy.

## Dane i warunki

Run: `20261104-192009-extract-fbd2cddad3674b1586cc896dedc71c64`. Zachowano wszystkie osiem plików bez zmian w [evidence](evidence/metadata.json); integralność opisuje [manifest SHA-256](evidence-manifest.json). Ocena do dalszego opracowania: [assessment.json](assessment.json). Ocenę semantyczną wykonał asystent względem źródeł i [oczekiwań ustalonych przed próbą](../../s3-extraction-v2.md).

Teksty MB-005 i tostów są identyczne bajtowo z poprzednim przebiegiem. W obu odpowiedziach są pełne JSON-y i `done_reason=stop`, bez błędów transportu, deserializacji i reguł kontraktu. Wszystkie cytaty występują dosłownie w odpowiednich źródłach, a odniesienia kroków wskazują istniejące składniki.

`s3-extraction-v2`, walidator podczas próby `recipe-draft-validation-v2`, schemat 1. Model `gemma4:12b`, Ollama 0.40.13, OLIWKA, .NET 10.0.14. `think=false`, temperatura 0, kontekst 8192, limit odpowiedzi 4096, JSON Schema w `format`. Każde żądanie zawiera wyłącznie instrukcję i bieżący tekst.

Hash instrukcji: `428441549B515AF2520151C09600F090D79A4B070516AA4EFB8B1D1D184BF7E6`.

Hash schematu: `1023A4BF8FABF8163593D1BFB17A92EA2DD5917AA78DF1C606B4D9B49A128121`.

Digest modelu i pozostałe dane środowiska znajdują się w zachowanych metadanych. Instrukcja ekstrakcji v2 pozostaje zamrożona po tej próbie.

## Ocena

| Wejście | Wynik odczytu modelu | Oryginalna walidacja C# | Ocena przepływu w czasie próby |
|---|---|---|---|
| MB-005 | Zgodny z oczekiwaniami | `needs_clarification`, jeden konflikt, bez błędów i uwag | Zaliczenie |
| Tosty | Zgodny ze źródłem, w tym czas etapu | `needs_clarification`, brak porcji, uwaga o „Trochę”, dodatkowo uwaga o „kromki” | Odstępstwo aplikacji: nadmiarowa uwaga o jednostce |

**MB-005:** 2 porcje, 15 minut, wszystkie pięć składników. Pomidory mają `amount.kind=unknown`, jednostkę g i puste pola liczbowe; `c1` zachowuje wersje 150 g i 200 g z dowodami. Mozzarella ma 125 g, oliwa 1 łyżkę, czosnek 5 g z określeniem sposobu posiekania. Kroki nie kopiują całkowitych ilości, lecz zachowują oryginały w `sourceText`. Kod prawidłowo prosi wyłącznie o rozstrzygnięcie konfliktu.

**Tosty:** 2 kromki chleba, 2 plastry goudy, ketchup `text: Trochę`. Brak porcji pozostaje `null`. Czas całkowity 0–10 odpowiada przyjętej konwencji „do 10 min”; `cooking=2–2`, pozostałe czasy etapów `null`. Kolejność czynności, czas i podanie z ketchupem zachowane w krokach. Model tym razem użył „kromki” zamiast „kromka” — to poprawna odmiana, bez zmiany ilości.

Nie zaliczamy nadmiarowej uwagi jako błędu odczytu modelu ani nie ukrywamy jej w ocenie aplikacji. Według oryginalnej walidacji tylko MB-005 spełnił dokładnie oczekiwany zestaw blokad. Po rozszerzeniu słownika oba **te same** zapisane dokumenty dają oczekiwane blokady w sprawdzeniu offline. To poprawa kodu, a nie trzeci wynik modelu.

## Czas i warunki pomiaru

| Odczyt | Czas końcowy | Ładowanie modelu | Przetwarzanie wejścia | Generowanie | Tokeny wejścia / odpowiedzi |
|---|---|---|---|---|---|
| MB-005 | 239,560 s | 27,034 s | 108,715 s | 103,316 s | 3374 / 1088 |
| Tosty | 198,950 s | 23,779 s | 102,984 s | 69,417 s | 3293 / 671 |

Mediana: 219,255 s; suma oczekiwania: 438,510 s. Między rozpoczęciami wywołań jest około 30 minut, podczas gdy żądania używają `keep_alive=10m`. Ponowne ładowanie jest zgodne z taką przerwą, choć logi nie pozwalają ustalić wszystkich przyczyn zużycia czasu. Nie traktujemy różnicy względem v1 jako kontrolowanego pomiaru kosztu zmiany instrukcji.

Opóźnienie nadal jest ograniczeniem użyteczności. Nawet samo generowanie zajmuje 69–103 s; kolejna próba podawana automatycznie ograniczy ręczne przerwy, ale nie gwarantuje krótkich odpowiedzi. Próg szybkości z dawnego benchmarku rozmów nie został tym wynikiem spełniony ani zniesiony; obecnie oceniamy odrębny prototyp ekstrakcji.

## Zmiany po próbie i weryfikacja

- Rozszerzono słownik znanych jednostek o podstawowe odmiany: „kromka/kromki/kromek”, „łyżka/łyżki/łyżek”, analogicznie łyżeczka, szklanka, ząbek i sztuka. Zachowano dotychczasowe odmiany plastrów. Nie przeliczamy ilości i nie zmieniamy tekstu odpowiedzi. Nieznane jednostki nadal wymagają przeglądu.
- Walidator ma wersję `recipe-draft-validation-v3`, zapisywaną w nowych metadanych. Oryginalny przebieg i jego pliki walidacji pozostają z v2.
- Sprawdzono offline oba nowe dokumenty: tortille mają tylko konflikt, tosty tylko brak porcji i przegląd „Trochę”. Stary błędny dokument z zakresem 150–200 g nadal jest odrzucany.
- Dodano automatyczne podawanie sześciu nowych wejść w `--extract ... --suite s3-extraction-check-v1`, z osobnymi źródłami, metadanymi i pustą kartą oceny jakości `review.json`. Oczekiwania nie trafiają do modelu. Nie ma automatycznych ponowień.

`dotnet run --project .\tests\MealBrain.ModelLab.Checks --no-restore` przeszedł. Obejmuje pięć przechwyconych odpowiedzi z obu prób, znane odmiany jednostek i zachowanie blokady starego konfliktu. Test automatycznego trybu z atrapą HTTP sprawdza sześć niezależnych wejść, zapis źródeł, błąd HTTP bez pominięcia dalszych przypadków, dokument z niedozwolonym polem i pozostawienie ocen semantycznych pustych. Nie wywołano rzeczywistego modelu.

Następny krok: [zamrożona próba szerszego zakresu ekstrakcji](../../s3-extraction-check-v1.md). Nowej instrukcji nie poprawiamy przed jej zakończeniem. Następnie podejmujemy decyzję o integracji z uzupełnianiem szkicu i osobno o pracy nad szybkością.
