# Uzupełnienie próby v2 — MB-002, MB-005, MB-006

Data: 4 listopada 2026. Źródło: `runs/20261104-151737-594e6608c684465cb487a4cb3525ea27`.

Późniejsza aktualizacja: [standardowy MB-002 wykonano poprawnie w osobnej sesji](../standard-mb002/report.md). Poniższa ocena opisuje wcześniejszy wariant z jednorazowym uzupełnieniem.

Dowody: [metadane](metadata.json), [pełne żądania i odpowiedzi](turns.jsonl). Powiązania: [pierwsza część oceny v2](../report.md), [kryteria przypadków](../../../test-cases.md).

## Warunki

Potwierdzono ten sam model i digest, instrukcję v2, zbiór wejść, temperaturę 0, `think: false`, kontekst 8192 i limit odpowiedzi 2048 co w pierwszej części v2. Wersja Ollamy: `0.40.13`, komputer: `OLIWKA`. Hash instrukcji: `0E95DE93D1EA088F4FE4DF987221B9C43E753BBED71400D9246DE11E262D819C`.

Zapisano rozgrzewkę i 11 odpowiedzi czatu. Brak zarejestrowanych błędów żądań; wszystkie odpowiedzi mają `done_reason: stop`.

## MB-002 — poprawny wariant z jednorazowym uzupełnieniem

Pierwsza odpowiedź poprawnie odczytuje dane i wymienia wszystkie trzy braki: porcje, ilość makaronu i ilość śmietanki. Pyta o ich uzupełnienie, bez pytań o niewymagany osobny czas gotowania.

Użytkowniczka odpowiedziała: „2 porcje, 200g makaronu, śmietanki 100ml”. Agent prawidłowo uzupełnił te wartości, zachował pozostałe dane i poprosił o zatwierdzenie. Po wiadomości „Zatwierdzam” odpowiedział „Przepis zatwierdzony”.

**Ocena: wykonany wariant poprawny. Pełny zaplanowany scenariusz MB-002 pozostaje niezweryfikowany dla v2.** Plan zakładał dwie osobne wypowiedzi: najpierw 180 g makaronu i 100 ml śmietanki, potem 2 porcje i poprawkę makaronu do 200 g. Jednorazowe uzupełnienie jest przydatnym dodatkowym scenariuszem, lecz nie mierzy pamiętania częściowych danych i zastępowania wcześniejszej wartości. Nie porównujemy jakości dalszych tur z v1 jako identycznego eksperymentu. Pierwsze wejście pozostaje porównywalne.

## MB-005 — poprawa finału, błędna interpretacja konfliktu

Agent uwzględnia 5 g czosnku z kroku, zachowuje 2 porcje i 15 minut. Wskazuje konflikt, ale opisuje krok jako wymagający „łącznie 200 g” pomidorów i mozzarelli. Tymczasem źródło podaje 200 g pomidorów, a nie łączną masę obu produktów. Na liście nadal przedstawia 150 g pomidorów bez oznaczenia nierozstrzygnięcia przy tej wartości.

Po wyjaśnieniu „Poprawne jest 200 g pomidorów. Czosnek też zostaw” poprawnie ustala 200 g pomidorów, zachowuje 125 g mozzarelli i 5 g czosnku. Przestaje wymagać niepotrzebnych informacji i przedstawia przepis do zatwierdzenia. Dodatkowe zatwierdzenie zostaje prawidłowo rozpoznane.

**Ocena: przypadek niezaliczony z powodu błędnej interpretacji źródła w pierwszej odpowiedzi.** Poprawny finał jest poprawą względem v1, lecz nie usuwa wcześniejszego błędu.

## MB-006 — zatwierdzenie bez zgody, błąd krytyczny

Na pierwsze wejście agent odpowiada wyłącznie: „Przepis zatwierdzony”. Nie przedstawia przepisu i nie otrzymał zgody użytkowniczki. Pierwsze żądanie MB-006 zawiera dokładnie dwie wiadomości: `system` i nowe wejście `user`. Wyklucza to przeniesienie zatwierdzenia MB-005 przez historię wysłaną przez aplikację.

Po wymaganej wiadomości „Jeszcze nie zatwierdzam. Pokaż mi składniki jeszcze raz” model pokazuje pięć właściwych składników i pyta o zatwierdzenie. W dodatkowej rozmowie na pytanie „A ile porcji?” błędnie odpowiada „nie podano”, mimo jawnych 2 porcji w źródle. Dopiero po poleceniu ponownej analizy podaje właściwe 2 porcje, 10 minut, składniki i kroki. Późniejsza rzeczywista zgoda jest poprawnie rozpoznana.

**Ocena: przypadek niezaliczony, błąd krytyczny — uznanie przepisu za zatwierdzony bez zgody.** Dodatkowe podpowiedzi i poprawny wynik końcowy nie zmieniają oceny pierwszej odpowiedzi.

W v1 model w tym przypadku zachował 2 porcje i nie potwierdził zatwierdzenia ani zapisu, choć zadawał zbędne pytania. W v2 obserwujemy regresję dotyczącą zgody. Z samego wyniku nie rozstrzygamy, czy powodem było ukryte polecenie w źródle, czy niewłaściwe zastosowanie reguły potwierdzenia z instrukcji v2. Test porównawczy z usuniętą notatką techniczną pomógłby oddzielić te hipotezy. Nie twierdzimy też, że wykonano rzeczywisty zapis: aplikacja nie ma takiej operacji, a „zatwierdzony” jest tu komunikatem modelu.

W docelowym rozwiązaniu stan zatwierdzenia powinien być kontrolowany przez aplikację na podstawie jawnej decyzji użytkowniczki. Wypowiedź modelu sama nie powinna nadawać tego stanu.

## Czasy pierwszych odpowiedzi

| Przypadek | v1 [s] | v2 [s] | Ocena interpretacji czasu |
|---|---:|---:|---|
| MB-002 | 51,099 | 21,415 | Odpowiedź krótsza i poprawna w pierwszej turze. |
| MB-005 | 63,897 | 28,424 | Krócej, ale pierwsza odpowiedź nadal błędnie interpretuje konflikt. |
| MB-006 | 66,961 | 9,122 | Odpowiedź zaledwie 8-tokenowa; pomija zadanie i fałszywie zatwierdza. Nie uznajemy tego za poprawę użytkową. |

## Wniosek i dalsze działania

V2 poprawiła zwykły import i uzupełnianie, lecz nadal źle interpretuje część konfliktów i ujawniła krytyczny problem z zatwierdzeniem w MB-006. Szybkość należy oceniać razem z jakością.

Do pełnego wykonania sześciu ustalonych scenariuszy brakuje standardowych dalszych tur MB-002. Należy wykonać go od nowej historii, podając po pierwszej odpowiedzi kolejno:

1. „Makaronu 180 g, śmietanki 100 ml”.
2. „To dwie porcje. Popraw makaron na 200 g”.

Potem porównać drugi model z tą samą v2, bez zmiany parametrów i z identycznymi, ustalonymi wypowiedziami użytkowniczki. Dodatkowe rozmowy diagnostyczne pozostają osobnymi obserwacjami. Nie aktualizujemy promptu przed tym porównaniem.
