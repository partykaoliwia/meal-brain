# S3 — podsumowanie pracy Oliwii na etap „Agent i model”

Stan: 4 listopada 2026. Zakończono bieżącą serię doboru modeli i rozwoju prototypu odczytu tekstu. Dostępne są kod C#, instrukcje, dane, pełne logi i oceny. Ostatnia regresja dała **5/6**, z jednym zmienionym cytatem wykrytym przez kod. To prototyp z jawnymi ograniczeniami, przygotowany do przekazania zespołowi.

## Cel i rezultat

Scenariusz S3 zaczyna się od nieuporządkowanego opisu przepisu. Przygotowany moduł zamienia tekst na wspólny szkic `RecipeDraft`, zachowuje niewiadome i konflikty, a następnie sprawdza dane i wskazuje braki w C#. Nie zakłada, że brak ilości wolno zastąpić typową wartością.

Model roboczy: **`gemma4:12b` w Ollamie**, temperatura 0, `think=false`, kontekst 8192, limit ekstrakcji 4096. Aktywna instrukcja ekstrakcji v3, kontrakt i JSON Schema w wersji 1, walidator v3. Stan zatwierdzenia ma być utrzymywany przez aplikację.

## Co sprawdzono i dlaczego zmieniliśmy podejście

Pierwsze próby obejmowały swobodną rozmowę z modelem: import, pytania o braki, poprawki i zgodę. Na sześciu przypadkach rozwojowych wyniki wyniosły: Qwen 3.5 4B — 2/6, Qwen 3.5 9B — 4/6, Gemma 4 E2B — 3/6, Phi-4 Mini — 0/6. Szczegóły i warunki: [porównanie modeli](model-comparison.md).

Następnie sprawdzono 12 nowych scenariuszy, 20 rozmów na model, z powtórzeniami trudnych sytuacji:

| Model | Zaliczone scenariusze | Rozmowy z zaobserwowanym błędem krytycznym |
|---|---|---|
| Gemma 4 E2B | 3/12 | 8/20 |
| Qwen 3.5 9B | 4/12 | 6/20 |
| Gemma 4 12B | 7/12 | 0/20 |

Żaden kandydat nie spełnił wszystkich ustalonych progów. Gemma 12B stała się kandydatem roboczym ze względu na najlepszy wynik tej próby i brak zaobserwowanych błędów krytycznych w tych 20 rozmowach. To uzasadnienie eksperymentalne, bez deklaracji ogólnej odporności. [Pełny raport finalistów](reports/2026-11-04-finalists-v1/report.md).

Problemy dotyczyły m.in. brakujących pytań i niespójnych ilości po poprawkach. Wprowadzono więc osobny moduł ekstrakcji: model opisuje dane w JSON, a aplikacja waliduje i wylicza kompletność. Zmieniono w ten sposób zakres odpowiedzialności modelu i kodu.

## Wyniki ekstrakcji

| Próba | Wniosek |
|---|---|
| Pierwszy odczyt trzech tekstów | Poprawny makaron; konflikt pomidorów błędnie zapisany również jako zakres; pominięty czas tostowania. |
| Instrukcja ekstrakcji v2, dwie regresje | Konflikt i czas tostowania poprawione; rozszerzono słownik jednostek aplikacji. |
| Sześć nowych wejść, instrukcja v2 | **2/6**. Cztery dokładne czasy stały się zakresami od zera; jeden cytat zmieniony. |
| Te same sześć wejść, instrukcja v3 | **5/6**. Cztery czasy poprawione; zmieniony cytat w kanapkach pozostał i został oznaczony do przeglądu. |

Ostatnie wyniki: [raport regresji 5/6](reports/2026-11-04-s3-extraction-check-v2/report.md). Poprawa dotyczy znanych danych rozwojowych. Próg jakości 6/6 nie został spełniony; nie zmieniamy go wstecz. Zgodnie z ustalonym z użytkowniczką zakresem zamykamy serię jako udokumentowany eksperyment na etap 2.

## Co rzeczywiście jest zaimplementowane

- `ExtractRecipeAsync(text)`: niezależny odczyt tekstu przez Ollamę, bez historii innych importów.
- Wspólne klasy i JSON Schema: składniki, ilości dokładne/zakresowe/opisowe/nieznane, czasy, kroki i konflikty.
- Kontrola struktury, wartości, identyfikatorów i dosłowności cytatów; wskazywanie braków w C#.
- Tryby ręczne i automatyczne, wersjonowanie warunków, logi oraz ręczne oceny jakości.
- Sprawdzenia mechaniki aplikacji z atrapą API, oddzielone od rzeczywistych testów modeli.

Odczyt nie zawiera operacji zatwierdzenia ani zapisu do bazy. Rozmowa uzupełniająca, import obrazów i pełny agent pozostają pracą zespołu. Konsola zawiera także starszy tryb rozmów użyty do badań modeli; nie należy utożsamiać go z gotowym przepływem opartym na szkicu.

## Ograniczenia i przekazanie

Walidacja struktury nie gwarantuje zgodności z tekstem. Model może zmienić cytat, pominąć informację lub zinterpretować ją błędnie. Ostatnia mediana wyniosła 68,766 s, a cały zestaw około 8 minut; wydajność wymaga dalszej pracy. Nazwy składników wymagają późniejszego ujednolicenia do wyszukiwania i sumowania zakupów.

[Przekazanie dla Marii i Patrycji](team-handoff.md) zawiera uruchomienie, punkt wejścia w C#, gotowe przykłady oraz ograniczony zakres ich eksperymentów na ten etap. Nie rozpoczynamy kolejnej serii testów S3 przed prezentacją tego materiału.

## Propozycja wypowiedzi na zajęciach

„Porównałyśmy lokalne modele w imporcie przepisu. Sama rozmowa nie zapewniła poprawnej kontroli braków i poprawek, więc wydzieliłyśmy odczyt do JSON i walidację w C#. Wybrałyśmy roboczo Gemmę 12B. Ostatnia regresja poprawionej instrukcji dała 5/6: czasy są już poprawne, ale model nadal zmienia jeden cytat, co wykrywa kod. Mamy uruchamialny moduł i opis ograniczeń; kolejnym krokiem zespołu jest rozmowa na szkicu i import obrazów do tego samego formatu.”
