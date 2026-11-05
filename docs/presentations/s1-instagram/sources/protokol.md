# S1 — protokół pierwszej próby na Instagramie

Przygotowano 5 listopada 2026 przed uruchomieniem badanych modeli. Wzorce oparto na wizualnym odczycie sześciu obrazów użytkowniczki przez asystenta. Nie są odpowiedziami badanej Gemmy ani Qwena. Zespół powinien przejrzeć transkrypcje i zaznaczone niejednoznaczności przy omawianiu wyników.

## Zakres danych

Wszystkie obrazy pochodzą z Instagrama. Oryginalne nazwy i bajty pozostają bez zmian. To pięć niekompletnych przepisów i jeden kadr bez treści przepisu, jeden materiał po angielsku i pięć po polsku. Nie ma tu niezależnego zestawu pilotażowego. Wykonujemy jedną próbę obu modeli przy istniejącym promptcie v1, bez dostrajania go na podstawie wyników tych obrazów. W razie późniejszej zmiany instrukcji ten zestaw staje się rozwojowy.

| ID | Oryginalny plik | Treść i oczekiwane zachowanie |
|---|---|---|
| S1-001 | IMG_5280.JPEG | Angielska lista składników batonów. Brak kroków, porcji i czasu; brak jednostki oleju; ułamki i ilości opisowe. |
| S1-002 | IMG_5281.JPEG | Makaron z łososiem. Dwie porcje, sześć ilości z listy, cztery przyprawy tylko w krokach, pieczenie ok. 25 min, bez total. |
| S1-003 | IMG_5283.JPEG | Bajgle. Cztery sztuki, brak tytułu i total; 1/2 łyżeczki soli, alternatywy mąki i posypki, odpoczynek i zakres pieczenia. |
| S1-004 | IMG_5284.PNG | Kaszka: lista bez przygotowania, 150 g oraz dodatkowe 20 g skyru. Nie dopisywać kroków z kadru filmu. |
| S1-005 | IMG_5285.PNG | Kadr potrawy bez przepisu: not_recipe. Nie zgadywać składników na podstawie wyglądu. |
| S1-006 | IMG_5286.JPEG | Kluski leniwe: mąka 60 g + ok. 3 g do obsypania, składniki w dalszej części, dogotowanie 1–2 min po wypłynięciu. |

Zestaw nie mierzy jeszcze obsługi stron internetowych, rozmytych cyfr, jawnych konfliktów ilości, ukrytych poleceń ani kompletnych przepisów z podanym czasem całkowitym. Nie przypisujemy tym obrazom dawnych ról sześciu pustych szablonów.

## Reguły oceny ustalone przed próbą

- Pełny wynik musi spełniać kryteria zapisane w przypisanym pliku references/S1-xxx.json, zachować dane widoczne na obrazie i nie mieć błędu krytycznego. Nie porównujemy identyfikatorów ani podziału zdań bajt po bajcie.
- Niekompletny przepis ma poprawnie zachować braki. needs_clarification może oznaczać zaliczenie całego przypadku. Odrzucenie rzeczywistej listy składników jako not_recipe jest błędem interpretacji.
- Błąd krytyczny: niepoparta źródłem istotna ilość, porcje, składnik albo etap przygotowania; wykonanie polecenia w źródle; zgoda lub deklaracja zapisu bez operacji. Pominięcie istotnej ilości też dyskwalifikuje przypadek, nawet gdy nie było zmyślenia.
- OCR oceniamy oddzielnie od interpretacji: błędnie odczytana cyfra to błąd odczytu, a utrata drugiego wystąpienia poprawnie odczytanego składnika to błąd interpretacji. Gdy nie da się tego rozstrzygnąć z odpowiedzi końcowej, opisujemy niepewność; nie dopisujemy modelowi niewidocznego toku rozumowania.
- sourceText jest transkrypcją obrazu i wymaga przeglądu. Parafraza jest dopuszczalna w text kroku, a nie w cytacie. Łamanie wierszy i odstępy w obrazie nie są wymaganiem identycznego formatowania JSON.
- W S1-003 źródło mówi o 4 sztukach, a schemat ma tylko servings. Dopuszczamy null przy ścisłej interpretacji porcji albo 4 jako jawny uzysk, z zachowaniem informacji o czterech sztukach. Nie oznacza to samodzielnie 4 osób. Wzorzec wykorzystuje null. Ten wyjątek ustalono przed próbą.
- W S1-004 dopuszczamy osobne 150 g i 20 g skyru lub wspólną ilość 170 g tylko przy zachowaniu obu części. W S1-006 zachowujemy 60 g mąki i około 3 g do obsypania; dopuszczalny jest inny podział struktur z zachowaniem tych danych i zastosowań.
- Przybliżony czas 25 min lub 5 min może mieć równe granice w schemacie, ale słowo około powinno pozostać w instrukcji. Nie tworzymy własnych granic zakresu. W S1-006 1–2 min dotyczy dogotowania po wypłynięciu, a nie całego procesu.
- Gramów mleka lub wody nie zamieniamy na ml. Samego cynamonu, soli lub słodzidła nie zamieniamy domyślnie na do smaku.
- Znany brak obsługi cup i angielskich opisów ilości przez walidator zapisujemy jako ograniczenie aplikacji, nie automatycznie jako błąd modelu. Zgodność samej struktury nie dowodzi poprawności OCR.

Warunki techniczne: modele gemma3:4b i qwen3-vl:4b-instruct, temperatura 0, kontekst 16384, limit odpowiedzi 4096, think pominięte, keep_alive=10m. Jeden odczyt na obraz i model, nowa historia każdego przypadku, bez rozgrzewki i bez automatycznych ponowień. Czas pierwszego przypadku może obejmować ładowanie modelu. Pomiary porównujemy informacyjnie, bez ogłaszania stabilności na podstawie jednego wykonania.

Manifest: evals/s1-images-v1/suite.json, identyfikator s1-instagram-v1. Zamrożenie zapisuje hashe przed uruchomieniem; nie zmieniamy instrukcji, wzorców ani obrazów po obejrzeniu odpowiedzi. Poprawki techniczne i ewentualne kolejne uruchomienia wymagają jawnego opisania w raporcie.
