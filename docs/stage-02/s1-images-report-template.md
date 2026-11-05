# S1 — porównanie importu przepisów z obrazów

Status: **szablon, brak wyników rzeczywistych modeli**. Wypełnić po wykonaniu prób; nie traktować narzędzi ani testów z atrapą jako wyniku OCR.

## Warunki

- Data, komputer, RAM/GPU:
- Ollama, .NET:
- Modele i digests (z metadata.json):
- Prompt i hash, schemat i hash, wersja walidatora:
- Zestaw i suite.lock.json:
- Temperatura, kontekst, limit odpowiedzi, tryb thinking:
- Preprocessing obrazów, liczba plików i ich pochodzenie:
- Rozgrzewka / ładowanie modelu, kolejność uruchomień:
- Ścieżki kompletnych katalogów z dowodami:

## Oczekiwania sprzed próby

Wzorce zapisano przed uruchomieniem: [data]. Każde wykonanie oceniono względem obrazu i przypisanej referencji. Opisać reguły zaliczenia i błędów krytycznych bez zmieniania progów po wynikach. Wskazać ewentualne powtórzenia; nie liczyć kilku wersji tego samego obrazu jako niezależnych przepisów.

## Wyniki

| Przypadek | Gemma: odczyt / interpretacja / cały przypadek | Qwen: odczyt / interpretacja / cały przypadek | Istotny błąd i dowód |
|---|---|---|---|
| S1-001 | do oceny | do oceny | |
| S1-002 | do oceny | do oceny | |
| S1-003 | do oceny | do oceny | |
| S1-004 | do oceny | do oceny | |
| S1-005 | do oceny | do oceny | |
| S1-006 | do oceny | do oceny | |

| Model | Ukończone technicznie | Poprawny kontrakt | Zaliczone przypadki | Przypadki z błędem krytycznym | Czasy |
|---|---|---|---|---|---|
| gemma3:4b | do pomiaru | do oceny | do oceny | do oceny | do pomiaru |
| qwen3-vl:4b-instruct | do pomiaru | do oceny | do oceny | do oceny | do pomiaru |

## Wnioski

- Roboczy wybór albo brak wystarczającego modelu i uzasadnienie:
- Błędy odczytu obrazu, oddzielone od błędów interpretacji:
- Nieczytelne dane, konflikty, braki, materiał bez przepisu:
- Różnice między poprawnością walidacji a zgodnością ze źródłem:
- Opóźnienie, awarie, ograniczenia sprzętu:
- Co przekazujemy Marii: dokumenty, obrazy, walidacje i wymagany przegląd:
- Ograniczenia małej próby i ewentualne wykorzystanie danych do poprawiania promptu:
