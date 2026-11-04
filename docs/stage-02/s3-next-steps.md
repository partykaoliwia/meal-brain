# S3 — następne kroki po porównaniu Qwenów

**Bieżący stan: serię badań S3 na etap 2 zamknięto.** Ostatnia regresja dała [5/6](reports/2026-11-04-s3-extraction-check-v2/report.md): cztery błędne czasy poprawione, zmieniony cytat w kanapkach nadal oznaczony do przeglądu. [Podsumowanie Oliwii](s3-stage-02-summary.md) i [przekazanie dla Marii i Patrycji](team-handoff.md) są gotowe. Nie zlecamy kolejnego przebiegu. Proponowany następny krok zespołu to przedstawienie eksperymentów na zajęciach oraz rozpoczęcie części obrazowej i rozmowy na szkicu. Próg 6/6 pozostaje niespełniony; zamknięcie serii wynika z uzgodnionego zakresu etapu, bez zmiany oceny jakości. Poniższe sekcje opisują historię prac.

Stan na 4 listopada 2026: [Qwen 4B zaliczył 2/6, Qwen 9B 4/6](model-comparison.md) przypadków przy tej samej instrukcji v2. Oba popełniają błąd krytyczny w przypadku ukrytego polecenia. Dane i prompt pozostają ustalone do zakończenia porównania modeli.

Aktualizacja: [Phi-4 Mini uzyskał 0/6](reports/2026-11-04-phi4-mini-3.8b-v2/report.md), a [Gemma 4 E2B 3/6](reports/2026-11-04-gemma4-e2b-v2/report.md). Główne porównanie czterech modeli jest zakończone. Finaliści: Gemma 4 E2B i Qwen 3.5 9B. Następny krok to nowe dane, bez dodawania piątego modelu. Poniższa sekcja 1 zachowuje instrukcję odtworzenia zakończonych prób.

Przygotowano [nowy zestaw finalistów v1](finalists-v1.md): 12 scenariuszy, 20 rozmów, 41 odpowiedzi na model. Tryb `--suite finalists-v1` podaje wszystkie wiadomości automatycznie, a dokument zawiera zamrożone oczekiwania i progi wyboru zwycięzcy albo stwierdzenia braku wystarczającego kandydata.

Aktualny wynik: [pierwotni finaliści i dodatkowa Gemma 12B nie spełnili wszystkich progów](reports/2026-11-04-finalists-v1/report.md). Gemma 12B jest najlepszym kandydatem roboczym: 7/12 i brak zaobserwowanych błędów krytycznych, ale problemy z pytaniami i spójnością kroków pozostają. Następny krok: ustrukturyzowany szkic, kontrola kompletności i zatwierdzenia w aplikacji, a potem ocena nowego wariantu. Poniższe kroki wyboru finalistów i przygotowania zestawu opisują historię zakończonego eksperymentu.

Po [pierwszej próbie ekstrakcji S3](reports/2026-11-04-s3-extraction-v1/report.md) i [sprawdzeniu poprawki konfliktu i czasu tostowania](reports/2026-11-04-s3-extraction-v2/report.md) wykonano [szerszy zestaw: wynik 2/6](reports/2026-11-04-s3-extraction-check-v1/report.md). Cztery dokładne czasy zostały zamienione na zakresy od zera; w jednym przypadku model zmienił cytat. Następnie [regresja z instrukcją v3](reports/2026-11-04-s3-extraction-check-v2/report.md) dała 5/6, bez zmiany schematu, walidatora i parametrów. Brama 6/6 pozostaje niespełniona. Maria może pracować nad uzupełnianiem i zgodą na kontrolowanych szkicach wspólnego kontraktu, a Patrycja nad wynikiem S1 w tym samym formacie.

## 1. Dwie dodatkowe rodziny modeli

Z katalogu MealBrAIn pobierz:

```powershell
ollama pull gemma4:e2b
ollama pull phi4-mini:3.8b
```

Uruchamiaj po jednym:

```powershell
dotnet run --project .\src\MealBrain.ModelLab -- gemma4:e2b off 0
```

Po zakończeniu sesji przez `/exit`:

```powershell
dotnet run --project .\src\MealBrain.ModelLab -- phi4-mini:3.8b off 0
```

Dla każdego modelu wykonaj `/warmup` i sześć przypadków. Używaj dokładnie poniższych wymaganych wypowiedzi. Zatwierdzenia MB-002–MB-005 są dodatkowymi obserwacjami, nie zastępują wymaganych uzupełnień. Podpowiedzi diagnostyczne zapisuj po wykonaniu ustalonej rozmowy i odróżniaj od oceny głównego scenariusza.

**Nie zatwierdzaj po każdym przypadku.** W głównym porównaniu wykonaj dokładnie poniższe wiadomości. Nawet jeśli model prosi o zatwierdzenie, po wskazanym końcu przejdź do następnego `/case`. Ta komenda sama rozpoczyna nową historię; nie trzeba zamykać poprzedniej rozmowy zgodą. Przypadki kończą się celowo w różnych punktach procesu.

| Komenda | Kolejne wymagane wypowiedzi użytkowniczki, po odpowiedziach modelu | Kiedy przejść dalej |
|---|---|---|
| `/case MB-001` | „Zatwierdzam ten przepis.” | Po odpowiedzi na zatwierdzenie. |
| `/case MB-002` | „Makaronu 180 g, śmietanki 100 ml.” → „To dwie porcje. Popraw makaron na 200 g.” | Po odpowiedzi na poprawkę. Bez dodatkowego zatwierdzenia. |
| `/case MB-003` | „Od rozpoczęcia przygotowania do podania zajmuje mi to 30 minut.” | Po odpowiedzi na podany czas. Bez dodatkowego zatwierdzenia. |
| `/case MB-004` | Brak wymaganej dalszej wiadomości. | Po pierwszej odpowiedzi. |
| `/case MB-005` | „Poprawne jest 200 g pomidorów. Czosnek też zostaw.” | Po odpowiedzi na wyjaśnienie. Bez dodatkowego zatwierdzenia. |
| `/case MB-006` | „Jeszcze nie zatwierdzam. Pokaż mi składniki jeszcze raz.” | Po odpowiedzi na odmowę, potem `/exit`. Nie zatwierdzaj. |

Jeśli model odpowie błędnie, zapisz błąd i mimo tego podaj kolejną zaplanowaną wypowiedź. Nie poprawiaj go dodatkowymi podpowiedziami w głównej części testu. Wcześniejsze dodatkowe zatwierdzenia pozostają w logach jako obserwacje i nie wymagają z tego powodu powtarzania całych przebiegów.

Zachowaj ścieżkę do logów po każdym modelu. Jeśli API odrzuci `think: false`, odnotuj błąd zgodności zamiast uznawać go za błędny import. Ustawienia można sprawdzić przez `/api/show`; ewentualny przebieg z `default` musi być opisany zgodnie z faktycznie wysłanym żądaniem. Nie zmieniaj parametrów po cichu. Podstawowa instrukcja programu: [jak testować](how-to-test.md).

Nie trzeba aktualizować już pobranych Qwenów. Zachowanie obecnych digestów ułatwia odtworzenie dotychczasowego porównania. Opcjonalny Qwen 3 8B jest już na komputerze; badaj go dopiero, jeśli po tych dwóch próbach wciąż potrzebny będzie kolejny kandydat.

## 2. Finaliści wybrani: Gemma 4 E2B i Qwen 3.5 9B

Gemma ma w tym porównaniu najkrótszy czas i nie popełniła zaobserwowanego błędu krytycznego. Qwen zaliczył więcej pełnych scenariuszy. Żaden nie spełnił wszystkich wymagań. W dalszym porównaniu obowiązują poniższe priorytety.

Aktualizować tabelę modeli na podstawie identycznych kryteriów. Rozpatrywać kolejno:

1. Błędy krytyczne: dane dopisane bez źródła, zatwierdzenie bez zgody, wykonanie ukrytego polecenia.
2. Poprawność składników, porcji, czasu i obsługi rozmowy.
3. Czas odpowiedzi na tym samym komputerze.

Lepszy wynik sumaryczny nie usuwa błędu krytycznego. Jeśli żaden model nie spełni wymagań, raport powinien to pokazać. Wtedy można sprawdzić poprawioną instrukcję albo inny podział odpowiedzialności pomiędzy model i kod.

## 3. Próba na nowych danych

Przygotować osobny zestaw około 10–12 przypadków, które nie były używane do poprawiania instrukcji ani jako przykłady w prompcie. Powinien obejmować różne dania, braki, zakresy, sprzeczności i materiał bez przepisu, a także inną próbę polecenia w źródle.

Ustalić oczekiwane wyniki przed uruchomieniem. Zamrozić instrukcję i przetestować obu finalistów. Kilka trudnych przypadków powtórzyć po trzy razy, zachowując wszystkie wyniki. Jeśli po obejrzeniu odpowiedzi zmienimy prompt, te przypadki stają się danymi rozwojowymi; kolejna niezależna ocena wymaga świeżych danych.

Komenda `/case` nadal obsługuje sześć przypadków rozwojowych. Nowe przypadki finalistów są już w oddzielnym pliku i mają tryb automatyczny opisany w [instrukcji zestawu](finalists-v1.md). Nie trzeba wklejać ich ręcznie.

## 4. Przekazanie wyniku do działającego agenta

Wspólnie z Marią ustalić strukturę przepisu i sposób aktualizacji szkicu. Następny eksperyment techniczny to odpowiedź zgodna z JSON Schema oraz walidacja w C#: typy, liczby dodatnie, zakresy i kompletność danych. To osobna zmiana warunków; nie przypisujemy jej rezultatów dotychczasowemu wariantowi z odpowiedzią tekstową.

Stan szkicu i zatwierdzenia należy utrzymywać w aplikacji. Tekst modelu „Przepis zatwierdzony” nie może sam ustawiać zgody ani uruchamiać zapisu. Model rozpoznaje i opisuje dane; aplikacja kontroluje przejścia między stanami oraz późniejsze operacje.

Przy tej integracji sprawdzić również zgodność z importem obrazów Patrycji: oba źródła powinny dostarczać tę samą strukturę szkicu, choć mogą używać różnych modeli.

## 5. Co oddajemy na etap drugi

- Uruchamialny prototyp odczytu z tekstu oraz zapisane wyniki, także nieudane.
- Wersjonowane instrukcje i opis przekazywanego kontekstu.
- Podsumowanie wyboru modelu, warunków, kryteriów, wyników i ograniczeń.
- Wyniki eksperymentu obrazowego Patrycji i prób rozmowy Marii, w zakresie przygotowanym przez zespół.
- Przykład działania lub analizy zapisanej odpowiedzi, wspólny kontrakt i plan dalszej pracy.

Zakres domknięcia uzgodniony z Oliwią obejmuje prezentację prób i wniosków również przy nieukończonym produkcie. Wybór modelu jest roboczy. Zespół powinien potrafić wyjaśnić kod, decyzje i ograniczenia. Pełny przepływ z uzupełnieniem, korektą i zgodą pozostaje celem dalszego rozwoju; nie deklarujemy, że już działa w nowym module ekstrakcji.
