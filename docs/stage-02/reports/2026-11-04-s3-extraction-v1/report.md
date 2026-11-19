# S3 extraction v1 — trzy pierwsze odczyty Gemmy 12B

Ocena z 4 listopada 2026. **MB-002 jest poprawny; MB-005 narusza kontrakt i został prawidłowo zatrzymany przez C#; dodatkowe tosty mają pominięty czas etapu.** Prototyp działa technicznie, ale nie jest jeszcze gotowym importem z uzupełnianiem i zatwierdzaniem.

## Materiał i metoda

Źródło: `runs/20261104-182757-extract-81d9369fa6e34a5a88090c51258a70f5`. Kopia wszystkich 11 plików bez zmian jest w [evidence](evidence/metadata.json); [manifest SHA-256](evidence-manifest.json) pozwala sprawdzić integralność. Oryginalne walidacje pozostawiono takie, jakie były w czasie próby.

Ocena do dalszego opracowania: [assessment.json](assessment.json).

Wariant `s3-extraction-v1`, model `gemma4:12b`, digest `6114515d63c17436a7c0417d82820ac65ad643e2806c5a3c89cb62846436ed0b`, Q4_K_M, Ollama 0.40.13, komputer OLIWKA, .NET 10.0.14. `think=false`, temperatura 0, `num_ctx=8192`, `num_predict=4096`, JSON Schema w `format`. Każdy import ma osobne dwa komunikaty: instrukcję i bieżące źródło. Nie było rozgrzewki ani automatycznych ponowień.

Hash instrukcji: `37DD54570F3E39A4615EBF7055831694B701A6B535111349D42203412C46DA62`.

Hash schematu: `1023A4BF8FABF8163593D1BFB17A92EA2DD5917AA78DF1C606B4D9B49A128121`.

Ocenę semantyczną wykonał asystent przez porównanie wszystkich trzech dokumentów ze źródłami. MB-002 i MB-005 są wcześniejszymi przypadkami rozwojowymi z uprzednio opisanymi oczekiwaniami. Tosty to dodatkowa obserwacja użytkowniczki; oczekiwania opisano po odczytaniu odpowiedzi. Te trzy wejścia nie są niezależnym benchmarkiem ani próbą pełnego agenta. Nie dodajemy ich do wyników finalistów v1.

## Wynik

| Import | Ocena danych względem źródła | Oryginalny stan z C# | Czas |
|---|---|---|---|
| 001 / MB-002, makaron | Zaliczenie: wszystkie pięć składników, braki i czas odczytane poprawnie | `needs_clarification`, dokładnie trzy braki | 206,068 s |
| 002 / MB-005, tortille | Niezaliczenie: konflikt 150/200 g błędnie stał się także zakresem | `invalid_output`, błąd blokujący | 117,037 s |
| 003 / tosty | Częściowo poprawny: brak `times.cooking=2–2`, mimo zachowania 2 minut w kroku | `needs_clarification`, jeden brak i dwie uwagi | 109,018 s |

Wszystkie trzy wywołania zakończyły się pełną odpowiedzią (`done_reason=stop`), bez błędów transportu i deserializacji. Dwa dokumenty przechodzą reguły struktury i wartości. **To nie znaczy, że dwa przepisy odczytano w pełni poprawnie:** walidator nie wykrył pominiętego pola czasu w tostach. Tylko MB-002 spełnia opisane oczekiwania odczytu; ma prawo pozostać niekompletnym przepisem, bo niekompletne było źródło. Nie wyliczamy procentowej skuteczności modelu z tej małej próby.

### MB-002 — prawidłowe niewiadome

Tytuł i 20 minut czasu całkowitego są poprawne. Ilości: 200 g szpinaku, 1 łyżka oliwy, sól `text: do smaku`, makaron i śmietanka `unknown`. Model nie zgadł liczby porcji. Zachował trzy kroki oraz dosłowne cytaty. Wszystkie identyfikatory w krokach istnieją.

C# prawidłowo wskazał tylko: porcje, ilość makaronu, ilość śmietanki. To oczekiwany wynik tej próby, a nie błąd z powodu `needs_clarification`.

### MB-005 — wykrycie konfliktu nie wystarczyło

Model zachował 2 porcje, 15 minut, wszystkie pięć składników, w tym 5 g czosnku obecne tylko w instrukcji. Konflikt `c1` prawidłowo wskazuje pomidory i dwa poparte źródłem warianty: 150 g oraz 200 g. Bieżący tekst kroku nie powtarza całkowitej liczby gramów, a oryginał zachowuje ją w `sourceText`.

Jednocześnie efektywna ilość pomidorów to `range`, 150–200 g. Źródło nie podaje takiego zakresu. Przy nierozstrzygniętym konflikcie ilość musi mieć `kind: unknown`, `value/min/max/text: null`, z zachowaną jednostką g. C# zwrócił dokładnie ten błąd i zablokował szkic. Nie wybieramy po cichu ilości ani nie zmieniamy kryterium tak, by zaliczyć odpowiedź.

Oczekiwany poprawny odczyt nadal ma `needs_clarification`: człowiek musi wybrać ilość pomidorów. `invalid_output` oznacza wcześniejszy problem z dokumentem dostarczonym przez model.

### Tosty — model, słownik aplikacji i nieprecyzyjne źródło

Model poprawnie odczytał dwie kromki chleba, dwa plastry goudy i opisową ilość ketchupu. Nie utożsamił dwóch kromek z dwiema porcjami. Kroki zachowują tostowanie przez 2 minuty i podanie z ketchupem; cytaty są zgodne ze źródłem.

Trzy osobne kwestie:

- **Błąd odczytu:** `times.cooking=null`, choć źródło podaje 2 minuty tostowania. Ten sam czas pozostaje w kroku, więc informacja nie zniknęła całkowicie. Obecny walidator nie sprawdza semantycznej kompletności czasów względem tekstu.
- **Brak w słowniku aplikacji:** jednostka `plastry` jest sensowna i pochodzi ze źródła. Uwaga o nieobsługiwanej jednostce wskazywała na zbyt wąski słownik C#, nie na złą ilość z modelu.
- **Polityka przeglądu:** `text: Trochę` prawidłowo zachowuje nieprecyzyjny opis. Aplikacja celowo wymaga jego przeglądu; można później potwierdzić ilość opisową albo podać konkretną. Nie należy zgadywać gramów.

`total=0–10` oddaje górne ograniczenie „maksymalnie 10 minut”, ale kontrakt v1 nie rozróżnia zakresu od jednostronnej granicy. Nie oceniamy tego jako dopisania dokładnego czasu 0 minut. W dokumentacji doprecyzowano konwencję: 0 jest technicznym minimum czasu, a użytkownikowi pokazujemy „do 10 min”. Dla przyszłej obsługi innych jednostronnych granic można rozważyć bogatszy typ czasu; tej próby nie przepisujemy wstecz na nowy format.

## Szybkość

Mediana z trzech importów: **117,037 s**; łączny czas oczekiwania: 432,123 s. Pierwszy odczyt obejmuje 22,709 s ładowania i 97,260 s przetwarzania wejścia. Następne ładowania zajmują około 0,058 i 0,123 s, a całe odczyty nadal prawie dwie minuty.

| Import | Tokeny wejścia | Tokeny odpowiedzi | Generowanie odpowiedzi | Tokeny/s generowania |
|---|---|---|---|---|
| 001 | 3035 | 912 | 85,882 s | 10,62 |
| 002 | 3077 | 1093 | 104,491 s | 10,46 |
| 003 | 2996 | 652 | 63,841 s | 10,21 |

Wartości z pól czasu API służą diagnostyce; czasy końcowe pochodzą ze stopera aplikacji. To inna instrukcja i dużo obszerniejsza odpowiedź niż w rozmowie finalistów. Nie porównujemy tych median jako równego testu modeli. Sama rozgrzewka nie usunie całego opóźnienia. Na razie rozstrzygamy poprawność; szybkość pozostaje ograniczeniem do osobnego pomiaru przed wyborem docelowej konfiguracji.

## Zmiany po obejrzeniu odpowiedzi

1. Zachowano cały przebieg, oryginalne oceny walidatora i instrukcję v1.
2. Dodano do słownika C# jednostkę `plaster` i odmiany `plastry`, `plastrów`, bez zmiany liczb ani przeliczania jednostek. Inne nieobsługiwane jednostki nadal wymagają przeglądu, a „Trochę” nadal nie jest automatycznie kompletne.
3. Przygotowano osobną instrukcję `extraction-prompt-v2.txt`: przykład konfliktu na innych danych, wymóg czasu etapu w `times` i kroku oraz objaśnienie górnej granicy czasu. Nowe uruchomienia zapisują wariant `s3-extraction-v2` i wersję walidacji. Schemat danych pozostaje w wersji 1; instrukcja dawnych rozmów porównawczych też jest niezmieniona.
4. Dodano sprawdzenia offline na wszystkich trzech rzeczywistych dokumentach: trzy braki makaronu, dalsze odrzucenie konfliktu i zniknięcie wyłącznie uwagi o jednostce w tostach. Nie naprawiają one brakującego czasu ani odpowiedzi modelu. Odpowiedzi zachowano w oryginale, bez cichej normalizacji konfliktu.

Weryfikacja: `dotnet run --project .\tests\MealBrain.ModelLab.Checks --no-restore` zakończone powodzeniem. Obejmuje dotychczasowy runner oraz reguły ekstrakcji, w tym trzy powyższe rzeczywiste odpowiedzi. Nie wywołano rzeczywistego modelu.

**Nie ma jeszcze wyników Gemmy dla instrukcji ekstrakcji v2.** Przejście testów kodu nie potwierdza poprawy modelu. Następny krok i zamrożone oczekiwania regresji są w [instrukcji v2](../../s3-extraction-v2.md). To ponowna próba rozwojowa na znanych danych; później potrzebny będzie szerszy sprawdzian i połączenie z uzupełnianiem szkicu.
