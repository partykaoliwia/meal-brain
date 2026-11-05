# S1 — pierwsze porównanie screenów z Instagrama

5 listopada 2026. **Żaden model nie zaimportował poprawnie całego przepisu w tej próbie.** Gemma 3 4B: 0/6 pełnych zaliczeń. Qwen3-VL 4B Instruct: 1/6, przy czym zaliczony jest wyłącznie screen bez przepisu. Właściwe importy przepisów: 0/5 dla obu modeli.

To wynik konkretnego promptu i ustawień na sześciu obrazach, nie ocena wszystkich możliwości tych rodzin modeli. Wzorce i kryteria zapisano przed uruchomieniem; nie zmieniano promptu ani danych po odpowiedziach. Wynik negatywny zachowujemy wraz z dowodami.

## Zakres i warunki

Wykorzystano sześć oryginalnych obrazów Patrycji: pięć niekompletnych przepisów i jeden kadr potrawy bez tekstu przepisu. Wszystkie pochodzą z Instagrama; jeden przepis jest po angielsku, pozostałe obrazy mają polski tekst lub interfejs. Brak jeszcze przypadków stron internetowych, rozmytego tekstu, jawnego konfliktu ilości i ukrytych poleceń. Nie było dodatkowego pilotażu ani dostrajania instrukcji w tej serii.

- [Protokół ustalony przed próbą](../../s1-instagram-v1-protocol.md): przypadki, kryteria i dopuszczalne interpretacje.
- Manifest i wzorce: `evals/s1-images-v1/`, zestaw `s1-instagram-v1`.
- Prompt S1 v1, SHA-256 `BB05676B8F76B5D2D9AEE6B336BFA53B6711AFA9CEEBA24C2E703B9D6E3FF639`.
- Schemat RecipeDraft v1, SHA-256 `1023A4BF8FABF8163593D1BFB17A92EA2DD5917AA78DF1C606B4D9B49A128121`; walidator `recipe-draft-validation-v3`.
- Ollama 0.40.11, .NET 10.0.13, komputer LAPTOP-PL4V658T. Nie kontrolowano osobno obciążenia ani użycia RAM/GPU.
- Temperatura 0, kontekst 16384, limit odpowiedzi 4096, `think` pominięte, `keep_alive=10m`, JSON Schema w `format`. Jeden obraz i dwie wiadomości na żądanie, bez historii innych importów.
- Kolejno Gemma, potem Qwen; po jednym wykonaniu na obraz. Bez rozgrzewki i automatycznych ponowień.
- Gemma digest `68ceab7e468a31c988b44d5a4984a0e21f58c4f7158c6ce29c1804babac38df7`.
- Qwen digest `84f7ed8042fa7cdbd902f7dcb7d0370e032095047cc3244179f263b77b5bc318`.

## Wynik

| Model | Kompletne odpowiedzi API | Poprawne reguły kontraktu | Pełne zaliczenia | Zaliczone importy przepisów | Przypadki z krytycznym zmyśleniem | Mediana czasu |
|---|---:|---:|---:|---:|---:|---:|
| gemma3:4b | 6/6 | 4/6 | **0/6** | **0/5** | 4/6 | 18,058 s |
| qwen3-vl:4b-instruct | 6/6 | 2/6 | **1/6** | **0/5** | 1/6 | 17,669 s |

Wszystkie odpowiedzi kończą się `done_reason=stop`; nie było błędów HTTP ani ucięcia przez limit tokenów. Wszystkie dwanaście odpowiedzi dało się sparsować do klas RecipeDraft. `invalid_output` dotyczyło reguł danych, np. jednoczesnego wypełnienia value/min/max/text, a nie błędu transportu lub składni JSON. Pole `not_recipe` może być strukturalnie poprawne, ale błędne semantycznie — tak było dla Qwena w S1-004.

Krytyczność oceniono zachowawczo dla jawnego zmyślenia danych składników, ilości lub etapów. Braki, niewłaściwe przypisanie czasu i niepoprawna struktura również dyskwalifikują przypadek, choć nie zwiększają automatycznie tej kolumny. Nie sumujemy kilku błędów jednego przypadku jako kilku niezależnych przypadków krytycznych.

## Poszczególne przypadki

| ID | Gemma | Qwen |
|---|---|---|
| S1-001 — angielskie batony | 3/4 zmienione na 0.375, dopisane cup do oleju i cztery niewidoczne kroki, w tym piekarnik 350°F/175°C. | Ułamki odczytane poprawnie i brak dopisanych kroków, lecz też dopisane cup do oleju oraz błędny kształt większości ilości. |
| S1-002 — makaron | Dwie porcje i sześć ilości poprawne; cztery przyprawy nie trafiły do ingredients. Zmienione cytaty, m.in. Do naczyń. | Ten sam brak przypraw, łosoś odczytany jako tosoś, pieczenie 25 min skopiowane do total; błędne pola exact. |
| S1-003 — bajgle | Zmyślony tytuł Babka, poważnie zniekształcony tekst i reprezentacja 1/2 łyżeczki; odpoczynek jako preparation, brak grilla. | Zachowane 4 jako uzysk i 0.5 soli, lecz m.in. tyżeczka i Ufóz w cytatach; cooking jako total, brak resting, błędne exact. |
| S1-004 — kaszka | Zachowane 150+20 g skyru, ale brak słodzidła; dopisany niewidoczny krok Wszystkie składniki wymieszać. | Błędne not_recipe: odrzucono rzeczywistą listę składników, ponieważ nie ma kroków. |
| S1-005 — sam kadr potrawy | Zmyślone m.in. 400 g fasoli, szklanka ryżu i cztery kroki. | **Zaliczony:** not_recipe, recipe=null i uzasadnienie, bez zgadywania przepisu. |
| S1-006 — kluski | Brak jednej porcji, cynamon z dopisaną ilością 1, łyżka skyru jako łyżka sypu, brak końcowego kroku, błędna reprezentacja mąki. | Jedna porcja poprawna, ale braki czasów jako 0/0, brak osobnej ilości 3 g mąki i składników końcowych; odwołanie do nieistniejącego i8. |

Pełne uzasadnienia i rozdzielenie odczytu od interpretacji: [Gemma — ocena](gemma3-4b/assessment.json), [Qwen — ocena](qwen3-vl-4b-instruct/assessment.json). OCR=NA przy odrzuceniu materiału, jeśli odpowiedź nie ujawnia transkrypcji przepisu. W S1-006 Qwena zmiany interpunkcji i zapisu zakresu naruszają wymaganą wierność sourceText; są mniej poważne niż pominięcia danych i błędy struktury. Złagodzenie tej oceny OCR nie zmieniłoby niezaliczenia przypadku.

## Czasy

| Przypadek | Gemma [s] | Qwen [s] |
|---|---:|---:|
| S1-001 | 31,311 | 25,157 |
| S1-002 | 17,589 | 16,675 |
| S1-003 | 24,738 | 31,669 |
| S1-004 | 13,824 | 4,480 |
| S1-005 | 16,102 | 4,490 |
| S1-006 | 18,527 | 18,662 |
| Mediana | 18,058 | 17,669 |
| Suma żądań | 122,091 | 101,133 |

Pierwszy odczyt obejmował według Ollamy około 9,292 s ładowania Gemmy i 4,176 s Qwena. Dwa szybkie wyniki Qwena są odrzuceniami, z których jedno jest błędne. Mniejsza suma czasu nie oznacza lepszego importu. Brak powtórzeń i kontroli sprzętu ogranicza porównanie wydajności.

## Wnioski i następny eksperyment

1. Screeny są użyteczne do oceny braków, ułamków, składników tylko w krokach i rozróżnienia zdjęcia potrawy od przepisu. Braki źródła powinny pozostać brakami. Nie oznacza to, że użytkowniczka ma dopisywać dane do screenów.
2. Bezpośredni obraz → złożony RecipeDraft przy obecnym promptcie nie daje poprawnego pełnego importu na tej próbie. Qwen lepiej zachował wybrane liczby i odrzucił sam kadr potrawy, ale nie wybieramy go jako gotowego modułu importu.
3. Walidator wykrywa błędny kształt ilości i nieistniejące identyfikatory. Nie potrafi porównać danych z pikselami: Gemma mogła oddać strukturalnie akceptowalny, całkowicie zmyślony przepis dla S1-005. Przegląd obrazu pozostaje niezbędny.
4. Oddzielne problemy aplikacji to polski słownik jednostek oraz brak rozróżnienia uzysku w sztukach od porcji. Nie przypisujemy ich automatycznie do jakości OCR.
5. Proponowany dalszy wariant: oddzielić wierną transkrypcję obrazu od mapowania na RecipeDraft, porównać ten przepływ oraz doprecyzować ograniczenia pól amount w schemacie. To propozycja kolejnego eksperymentu, jeszcze niewykonana. Obecnych wyników nie poprawiamy po cichu; późniejsza niezależna ocena powinna użyć nowych obrazów.

## Zachowane dowody

- Gemma: [surowy przebieg](gemma3-4b/evidence/metadata.json), [odpowiedzi](gemma3-4b/evidence/turns.jsonl), [oryginalna pusta karta](gemma3-4b/evidence/review.json), [oceny](gemma3-4b/review.filled.json), [manifest plików](gemma3-4b/evidence-manifest.json).
- Qwen: [surowy przebieg](qwen3-vl-4b-instruct/evidence/metadata.json), [odpowiedzi](qwen3-vl-4b-instruct/evidence/turns.jsonl), [oryginalna pusta karta](qwen3-vl-4b-instruct/evidence/review.json), [oceny](qwen3-vl-4b-instruct/review.filled.json), [manifest plików](qwen3-vl-4b-instruct/evidence-manifest.json).
- [Podsumowanie maszynowe](summary.json); w każdym katalogu modelu także scorecard.filled.csv.

Oba archiwa zawierają po 38 plików: obrazy, wzorce, szkice, walidacje, prompt, schemat, metadane i zamrożony protokół. Oryginalne przebiegi w runs pozostawiono bez zmian; oceny zapisano osobno. Wyniki i wzorce oceniał asystent na podstawie pełnych odpowiedzi i obrazów; zespół może przejrzeć decyzje i dowody.
