# MealBrAIn S1 — prezentacja do Overleafa

9 slajdów po polsku, format 16:9, plan na około 5–7 minut. Projekt zawiera kod Beamera, slajd z dosłownymi fragmentami użytego promptu i wszystkie sześć oryginalnych screenów. Obrazy skopiowano bez zmiany ich treści.

## Uruchomienie

Zaimportuj archiwum ZIP jako projekt w Overleafie. Plik główny to `main.tex`. Ustaw kompilator **pdfLaTeX**. Źródło zawiera również obsługę XeLaTeX/LuaLaTeX z fontami Latin Modern.

Zachowaj katalog `images/` obok pliku `main.tex`; sam kod bez obrazów nie da pełnej prezentacji. Notatki do wystąpienia znajdziesz w `notatki.md` oraz w poleceniach `\note{...}` w kodzie. Domyślny PDF pokazuje wyłącznie dziewięć slajdów.

## Zawartość

- `main.tex` — edytowalny kod prezentacji;
- `images/` — screeny z Instagrama;
- `notatki.md` — propozycja wypowiedzi do każdego slajdu;
- `sources/` — raport, protokół, podsumowanie, wybrane pełne odpowiedzi modeli oraz pełny prompt S1 v1, schemat JSON i stała wiadomość użytkownika.

Slajd z promptem pokazuje dosłowne fragmenty. Pełna użyta instrukcja jest w `sources/image-extraction-prompt-v1.txt`; schemat dołączany do wiadomości systemowej w `sources/recipe-draft-v1.schema.json`, a wiadomość użytkownika w `sources/wiadomosc-uzytkownika.txt`.

Raport w sources jest kopią z repozytorium. Jego odnośniki do pełnych archiwów odnoszą się do repozytorium, nie do tego mniejszego pakietu. Wybrane przykłady użyte na slajdach są dostępne w tym projekcie.

## Kolejność slajdów

1. Cel importu ze screena.
2. Metoda i ustawienia testu.
3. Fragmenty promptu użytego w badaniu.
4. Materiał badawczy i rodzaje braków.
5. Batony: ułamki i dopisywanie danych.
6. Sam kadr potrawy: poprawne odrzucenie.
7. Składnia JSON i reguły kontraktu.
8. Wyniki i czasy odpowiedzi.
9. Wnioski, ograniczenia i następny eksperyment.

## Ważne przy prezentowaniu

Gemma ma 0/6 pełnych zaliczeń, Qwen 1/6. Jedyny zaliczony przypadek Qwena to odrzucenie kadru bez przepisu. **Poprawny pełny import: 0/5 dla obu modeli.** Wyniki dotyczą jednej konfiguracji i jednego przebiegu na sześciu screenach. Proponowany wariant z oddzielnym odczytem tekstu nie był jeszcze testowany.

## Pochodzenie obrazów

| Plik w prezentacji | Oryginał w zestawie Patrycji |
|---|---|
| images/protein-bars.jpg | IMG_5280.JPEG |
| images/makaron.jpg | IMG_5281.JPEG |
| images/bajgle.jpg | IMG_5283.JPEG |
| images/kaszka.png | IMG_5284.PNG |
| images/kadr-potrawy.png | IMG_5285.PNG |
| images/kluski.jpg | IMG_5286.JPEG |

Źródła: lokalne materiały projektu MealBrAIn, raport S1 z 5 listopada 2026 i screeny dostarczone przez Patrycję. Oryginalnych adresów postów nie podano, dlatego nie dopisujemy zgadywanych linków.
