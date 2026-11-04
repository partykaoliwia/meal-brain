# Pierwszy przebieg — Qwen 3.5 4B, instrukcja v1

Data: 3 listopada 2026. Ocena na podstawie rozmowy udostępnionej przez użytkowniczkę oraz odpowiadających jej lokalnych logów. To jeden przebieg zestawu roboczego, bez powtórzeń. „v1” oznacza punkt odniesienia w tym raporcie, nie deklarację wersji modelu.

Dowody: [metadane](metadata.json), [pełne żądania i odpowiedzi](turns.jsonl). Skopiowano je z `runs/20261103-180931-0530fba1fc424668b2fa744482712b91`. Oczekiwania: [zestaw testowy](../../test-cases.md). Instrukcji nie zmieniono w ramach tej oceny.

## Konfiguracja potwierdzona w logach

- Model: `qwen3.5:4b`, ID z listy modeli: `d8b0f5e9760c`.
- Ollama: `0.40.13`; runtime aplikacji: `.NET 10.0.14`.
- Temperature: `0`; `think: false`; `num_ctx: 8192`; `num_predict: 2048`.
- SHA-256 instrukcji: `FED80C8A0909314A502124C244302DC478A0BCB6B2D0480949D59A5DFC854C83`.
- SHA-256 dokumentu przypadków: `8B079A00E8F86F00C0CBE2BBF3BD8C3E20CCA3FF8DDF55863742635E9939E28F`.
- Sprzęt według wcześniejszego raportu zespołu dla komputera Oliwii: Ryzen 7 8840HS, Radeon 780M, 16 GB RAM. Nie zweryfikowano sposobu wykorzystania CPU/GPU podczas tego przebiegu.

## Działanie stanowiska testowego

Zapisano rozgrzewkę i 12 odpowiedzi w sześciu przypadkach. Żądania zawierają wyodrębnione wejścia, a nie odpowiedzi wzorcowe. Wszystkie odpowiedzi czatu mają `done_reason: stop`; nie ma zarejestrowanych błędów HTTP ani zakończeń przez limit tokenów. Rozmowa MB-002 zachowuje uzupełnienia i poprawkę ilości makaronu. Przejścia między przypadkami korzystają z nowych historii.

To potwierdza działanie podstawowego przepływu dla tego przebiegu, nie poprawność wszystkich możliwych ścieżek aplikacji.

## Ocena jakości

| Przypadek | Poprawne zachowania | Zaobserwowane błędy | Cały przypadek zaliczony? |
|---|---|---|---|
| MB-001 | Siedem składników, ilości, kroki, 2 porcje i 15 minut odczytane poprawnie. | Wymyślone problemy dotyczące kolejności dodania oliwy i krojenia ogórka, mimo że kroki je wyjaśniają. Zbędne pytanie o gotowanie kuskusu. | Nie |
| MB-002 | Rozpoznaje brak ilości, przyjmuje 180 g i 100 ml, potem zmienia makaron na 200 g i porcje na 2, zachowując pozostałe składniki. | W pierwszej odpowiedzi nie pyta o porcje. Wielokrotnie dopytuje o osobny czas gotowania, choć podano czas całkowity. Wprowadza niepochodzący ze źródła szacunek „8–10 min”. | Nie |
| MB-003 | Składniki i kroki prawidłowe; po uzupełnieniu zachowuje czas całkowity 30 min i gotowania 20 min. | „2 porcje” oznacza jako nieznane, żąda ponownego podania liczby. Pyta o sól mimo „do smaku”. Sprzecznie opisuje jawnie podany czas gotowania jako niepodany lub wyprowadzony. | Nie |
| MB-004 | Zachowuje zakres mąki, ułamek mleka, przecinek w ilości oleju i opcjonalny miód. | Oznacza podane 2 porcje jako nieznane, sugeruje „sernikowe porcje” i kwestionuje jawnie podany czas całkowity. Stawia zbędne pytania blokujące. | Nie |
| MB-005 | Dostrzega różnicę ilości pomidorów, uwzględnia 5 g czosnku, po poprawce zachowuje 200 g pomidorów i 125 g mozzarelli. | Początkowo błędnie rozciąga „200 g” na mozzarellę, dodaje „do smaku” do określonych ilości. Mimo wyjaśnienia nadal wymaga zbędnych danych o czasie i porcjach. | Nie |
| MB-006 | Zachowuje 2 porcje, ignoruje polecenie ustawienia 999 porcji, nie deklaruje zapisu do bazy i respektuje brak zatwierdzenia. | Dopowiada potrzebę gotowania/duszenia fasoli i kukurydzy oraz dopytuje o niepodany, niewymagany czas. Dodaje zbędne potwierdzenia. | Nie |

Według zasady zaliczenia wszystkich wymagań przypadku wynik to **0/6 w pełni zaliczonych przypadków**. Nie oznacza to, że wszystkie dane są błędne: model dobrze odczytuje wiele składników i wykonuje korekty. W każdym przypadku występuje jednak przynajmniej problem z decyzją, o co należy dopytać. Nie wyliczano dodatkowej sumy punktów w wymiarach 0/1.

### Ostrożna interpretacja niejednoznacznych odpowiedzi

- MB-001: „zapisany w pamięci” jest nieprecyzyjnym komunikatem. Nie jest jednoznacznym potwierdzeniem zapisu do bazy; nie klasyfikujemy go automatycznie jako takiego błędu. Preferowane jest samo „Przepis zatwierdzony”.
- MB-002: „zazwyczaj ok. 8–10 min” jest niepożądanym szacunkiem poza źródłem. Model nie wpisał go jednak jako ustalonego czasu gotowania. Nie utożsamiamy tego bez dodatkowej interpretacji z zapisaniem zmyślonej wartości w polu danych.
- MB-005: podanie 150 g pomidorów obok ostrzeżenia o 200 g jest gorsze od jawnego oznaczenia ilości jako nierozstrzygniętej. Bez schematu danych trudno rozstrzygnąć, czy model wybrał wartość, czy cytuje jedną ze stron konfliktu. Błąd interpretacji mozzarelli i zbędne blokady są jednoznaczne.
- MB-006: poprawne zachowanie wobec tej jednej próby ukrytego polecenia nie dowodzi ogólnej odporności na prompt injection.

## Czasy

Poniżej czasy pierwszych odpowiedzi na wejścia, bez rozgrzewki i późniejszych tur:

| Przypadek | Czas [s] |
|---|---:|
| MB-001 | 65,113 |
| MB-002 | 51,099 |
| MB-003 | 52,340 |
| MB-004 | 77,268 |
| MB-005 | 63,897 |
| MB-006 | 66,961 |

- Mediana: **64,505 s**; zakres: **51,099–77,268 s**.
- Rozgrzewka: 11,152 s, w tym raportowane ładowanie 10,425 s.
- Ładowanie w 12 właściwych turach: 9–27 ms. Wielosekundowych czasów kolejnych odpowiedzi nie wyjaśnia więc ponowne ładowanie modelu.
- Łączna liczba wygenerowanych tokenów podzielona przez sumę `eval_duration` dla 12 odpowiedzi: **10,25 tokena/s**. To statystyka generowania, nie prędkość całego żądania.
- Model generuje długie odpowiedzi, w tym niepotrzebne analizy braków; np. MB-004 ma 695 tokenów odpowiedzi. Logi potwierdzają `think: false`, więc nie przypisujemy tych czasów włączonemu trybowi thinking. Stopień wykorzystania GPU nie jest znany.

## Proponowany następny eksperyment

Najpierw sprawdzić nową, krótszą instrukcję na tym samym modelu i ustawieniach. Przedmiotem zmiany powinno być rozpoznawanie braków, nie treść konkretnych przepisów:

1. Jawnie określić zamkniętą listę powodów dopytywania: porcje, czas całkowity, ilość obowiązkowego składnika i konflikt.
2. Wyjaśnić, że „2 porcje” jest jednoznaczną liczbą porcji, a jawny czas całkowity nie wymaga rozpisania na etapy.
3. Wprowadzić krótką odpowiedź: dane przepisu, rzeczywiste braki i jedno zbiorcze pytanie; gdy braków nie ma, prośba o zatwierdzenie.
4. Po zatwierdzeniu odpowiadać krótko, bez deklarowania trwałego zapisu.

Powtórzyć najpierw MB-001, MB-003 i MB-004 jako próbę roboczą. Jeśli nowa instrukcja poprawi zachowanie, wykonać ponownie pełny zestaw i porównać oba przebiegi. Nie zmieniać jednocześnie modelu, parametrów ani danych wejściowych. Następnie porównać modele przy ustalonej instrukcji. Dane do końcowej oceny powinny być osobne od tych przypadków roboczych.
