# Pierwszy zestaw — sześć screenów z Instagrama

Obrazy już dodano. Zachowujemy oryginalne nazwy; manifest przypisuje je do przypadków:

| Przypadek | Plik | Zawartość |
|---|---|---|
| S1-001 | IMG_5280.JPEG | Angielska lista składników batonów |
| S1-002 | IMG_5281.JPEG | Makaron z pieczonym łososiem |
| S1-003 | IMG_5283.JPEG | Bajgle, składniki i kroki |
| S1-004 | IMG_5284.PNG | Kaszka, lista bez instrukcji z filmu |
| S1-005 | IMG_5285.PNG | Sam kadr potrawy bez przepisu |
| S1-006 | IMG_5286.JPEG | Kluski leniwe, składniki w kilku miejscach |

Wzorce zapisano na podstawie obrazów przed próbą modeli. [Protokół Instagrama](../../../docs/stage-02/s1-instagram-v1-protocol.md) opisuje rzeczywisty zakres, kryteria i niejednoznaczności. Nie ma jeszcze osobnych obrazów pilotażu. Poniżej zachowano pierwotny plan rozszerzenia zestawu; nie jest opisem pokrycia obecnej próby.

## Pierwotny plan dodatkowych przypadków

Zapisz je dokładnie pod nazwami poniżej. Jeden plik = jeden przypadek.

| Plik | Jaki screen |
|---|---|
| `S1-001.png` | Czytelny opis posta na Instagramie |
| `S1-002.png` | Czytelna strona internetowa |
| `S1-003.png` | Przepis z brakami |
| `S1-004.png` | Częściowo nieczytelny lub ucięty przepis |
| `S1-005.png` | Sprzeczne ilości |
| `S1-006.png` | Materiał bez przepisu |

Czytelne screeny powinny obejmować widoczne składniki i przygotowanie. W Instagramie rozwiń opis. W przypadku z brakami lub ucięciem zachowaj celowo niepełne dane. Nie dopisuj porcji ani czasu do oryginału, aby sztucznie uzyskać kompletność.

Dwa dowolne dodatkowe screeny do pilotażu przechowuj w sąsiednim katalogu `pilot/`. Nie używaj sześciu screenów oceny do poprawiania promptu przed porównaniem. Zachowaj tę samą wersję każdego pliku dla obu modeli. Jeśli potrzebny jest JPEG, zmień pole `image` w `suite.json` przed zamrożeniem.

Przed testami uzupełniamy wzorce w `references/`. Samo dodanie obrazów nie oznacza przygotowania oczekiwanych odpowiedzi. Szczegóły: [instrukcja S1](../../../docs/stage-02/s1-images-how-to.md).
