# Eksperyment roboczy: instrukcja v1 → v2

Przygotowano 4 listopada 2026. Status: sześć ustalonych scenariuszy wykonano w trzech sesjach v2, łącznie 2/6 zaliczonych. [Raport porównania v1 i v2](reports/2026-11-04-qwen3.5-4b-v2/report.md) obejmuje również [uzupełnienie MB-002, MB-005, MB-006](reports/2026-11-04-qwen3.5-4b-v2/remaining-cases/report.md) i [poprawnie wykonany standardowy MB-002](reports/2026-11-04-qwen3.5-4b-v2/standard-mb002/report.md). Pozostają błędy pytań, konfliktu i zatwierdzenia bez zgody. Następny krok: porównanie drugiego modelu przy tej samej v2.

Aktualizacja porównania modeli: [Qwen 9B przy v2 zaliczył 4/6](reports/2026-11-04-qwen3.5-9b-v2/report.md), ale nadal popełnił błąd krytyczny w MB-006. Bieżący plan to [Gemma 4 E2B i Phi-4 Mini](s3-next-steps.md); zbiorcze wyniki znajdują się w [porównaniu S3](model-comparison.md).

## Cel i hipoteza

W [pierwszym przebiegu](reports/2026-11-03-qwen3.5-4b-v1/report.md) model wielokrotnie zgłaszał nieistniejące braki. Hipoteza: zamknięta lista powodów dopytywania i prostsza forma odpowiedzi ograniczą zbędne pytania bez pogorszenia odczytu danych.

Zmiana dotyczy wyłącznie instrukcji systemowej: [archiwalna v1](prompts/system-prompt-v1.txt) → [aktywna v2](../../src/MealBrain.ModelLab/system-prompt.txt). Archiwalną v1 odtworzono dokładnie z metadanych pierwszego przebiegu; jej SHA-256 zgadza się z raportem. Nie wprowadzamy przykładów konkretnych przepisów do instrukcji.

W v2:

- Dopytywanie ma cztery powody: brak porcji, czasu całkowitego, ilości obowiązkowego składnika lub rzeczywista sprzeczność.
- Jawnie podana liczba porcji jest wystarczająca. Czas całkowity nie wymaga podziału na etapy.
- Osobny czas gotowania pokazujemy tylko wtedy, gdy występuje w źródle.
- Odpowiedź kończy się rzeczywistymi brakami i zbiorczym pytaniem albo prośbą o zatwierdzenie.
- Potwierdzenie zatwierdzenia jest krótkie i nie deklaruje zapisu.

To zmiana kilku elementów jednej instrukcji. Badamy skuteczność wariantu jako całości; z tego eksperymentu nie ustalimy, które pojedyncze zdanie spowodowało różnicę.

## Co pozostaje stałe w porównaniu

- Model `qwen3.5:4b`, ten sam pobrany wariant/digest co w v1.
- Thinking `off`, temperatura `0`, kontekst 8192, limit odpowiedzi 2048.
- Ten sam komputer i wersja Ollamy, bez aktualizacji modelu w trakcie porównania.
- Te same wejścia, uzupełnienia i kryteria oceniania; nowa historia dla każdego przypadku.

## Uruchomienie próby

Zakończ poprzednią aplikację przez `/exit`, jeśli nadal działa. W PowerShellu, z katalogu repozytorium:

```powershell
dotnet run --project .\src\MealBrain.ModelLab -- qwen3.5:4b off 0
```

Uruchomienie bez `--no-build` kopiuje aktualny plik instrukcji do katalogu programu. Instrukcja jest wczytywana przy starcie, więc sama edycja pliku podczas działającej sesji jej nie zmieni. Nowe metadane zapiszą rzeczywistą treść i hash v2.

Przy `Ty>` wpisuj poniższe komendy po jednej, czekając na odpowiedź.

### MB-001: kompletny przepis

```text
/warmup
/case MB-001
```

Po otrzymaniu przepisu:

```text
Zatwierdzam ten przepis.
```

Sukces: poprawne składniki i dane, brak pytań o gotowanie kuskusu, krojenie ogórka lub moment dodania oliwy. Po zatwierdzeniu krótkie potwierdzenie bez twierdzenia o zapisie.

### MB-003: rzeczywiście brakujący czas całkowity

```text
/case MB-003
```

Po odpowiedzi:

```text
Od rozpoczęcia przygotowania do podania zajmuje mi to 30 minut.
```

Sukces: od początku porcje = 2, gotowanie = 20 min. Pytanie wyłącznie o brakujący czas całkowity, bez pytań o sól ani podane porcje. Po uzupełnieniu czas całkowity = 30 min, gotowanie nadal = 20 min, przepis gotowy do przeglądu.

### MB-004: zakresy i ilości opisowe są kompletne

```text
/case MB-004
```

Sukces: porcje = 2, czas całkowity = 20 min, zakres mąki zachowany, mleko = pół szklanki, olej = półtorej łyżki, cynamon do smaku, miód opcjonalny. Brak pytań o wielkość porcji, czas smażenia lub dokładną ilość mąki/cynamonu.

Zakończ przez `/exit` i zachowaj ścieżkę do nowego katalogu `runs`. Pełne kryteria nadal obowiązują zgodnie z [zestawem przypadków](test-cases.md).

## Ocena i dalsza decyzja

Porównaj odpowiedzi z v1: czy zniknęły zbędne pytania i czy nie pogorszyły się dane? Zapisz również czasy; szybsza odpowiedź nie rekompensuje błędu. To trzy przypadki robocze, więc nie wyciągamy z nich wniosków o ogólnej jakości modelu.

Jeżeli v2 poprawi te zachowania, uruchom pełne sześć przypadków, szczególnie MB-002 (rzeczywiste braki), MB-005 (konflikt) i MB-006 (polecenie w źródle). To sprawdzi, czy ograniczenie pytań nie spowodowało pomijania potrzebnych wyjaśnień. Jeśli próba nie poprawi jakości, opisz błędy i sprawdź drugi model z tą samą instrukcją zamiast bez końca dostosowywać prompt do tych samych przepisów.

Porównanie modeli zaczynamy po ustaleniu jednej wspólnej instrukcji. Osobny, niewykorzystywany do poprawiania promptów zestaw będzie potrzebny do końcowej oceny.
