# Propozycja kontraktu: RecipeDraft v1

Wspólny wynik odczytu przepisu z tekstu (S3) albo obrazu (S1). Jest to propozycja do uzgodnienia przez zespół, z pierwszym prototypem odczytu S3 opisanym poniżej. Nie zmienia aktywnego promptu v2 ani wyników dotychczasowych testów.

Przykład pełnego dokumentu: [recipe-draft-v1.example.json](recipe-draft-v1.example.json).

Stan prototypu: przygotowano [JSON Schema](recipe-draft-v1.schema.json), klasy C# i osobny tryb ekstrakcji S3. [Instrukcja pierwszego uruchomienia](../stage-02/s3-extraction-v1.md). Kontrakt pozostaje propozycją do wspólnego uzgodnienia przez zespół; aktywny prompt v2 testów porównawczych jest zachowany.

Aktualizacja kończąca serię na etap 2: [podsumowanie S3](../stage-02/s3-stage-02-summary.md), wynik regresji 5/6 i [przekazanie dla zespołu](../stage-02/team-handoff.md). Bieżąca ekstrakcja używa instrukcji v3 i walidatora v3, przy tym samym schemacie 1. Prototyp wymaga dalszego przeglądu i integracji; nie rozpoczynamy kolejnej serii testów w tym domknięciu etapu.

## Podział odpowiedzialności

Model zwraca odczytane dane, niewiadome i konflikty. Aplikacja przechowuje źródło, nadaje identyfikator importu, waliduje dane, wylicza brakujące pola i przechowuje zgodę użytkowniczki. Z tego kontraktu korzysta zarówno odczyt tekstu Oliwii, jak i odczyt obrazu Patrycji; Maria obsługuje późniejsze uzupełnianie i stan rozmowy.

Zgodność ze schematem potwierdzi kształt danych, nie ich zgodność z przepisem. Odczyt nadal wymaga testów semantycznych.

## Korzeń dokumentu

```json
{
  "schemaVersion": 1,
  "inputKind": "recipe",
  "rejectionReason": null,
  "recipe": {}
}
```

- `schemaVersion`: stała liczba całkowita `1`.
- `inputKind`: `recipe` albo `not_recipe`.
- `rejectionReason`: `null` dla przepisu, krótki powód dla materiału bez przepisu.
- `recipe`: obiekt poniżej dla przepisu, `null` dla materiału bez przepisu.

Obiekt `{}` w skrócie powyżej oznacza miejsce na opisane niżej pola, nie poprawny kompletny wynik.

Przykład dla reklamy warsztatów:

```json
{
  "schemaVersion": 1,
  "inputKind": "not_recipe",
  "rejectionReason": "Ogłoszenie nie zawiera składników ani instrukcji przygotowania potrawy.",
  "recipe": null
}
```

Nie tworzymy pustego dania o tytule reklamy. Fragment rzeczywistego przepisu, np. z uciętą ilością, może natomiast mieć `inputKind: recipe` i niekompletne dane.

## Pola recipe

| Pole | Typ i znaczenie |
|---|---|
| `title` | Tekst albo `null`, jeśli brak tytułu. Bez wymyślania nazwy. |
| `servings` | Dodatnia liczba całkowita albo `null`, gdy nie wiadomo, na ile porcji jest danie. Liczby na etykiecie produktu nie są porcjami przepisu. |
| `times` | Obiekt z polami `total`, `preparation`, `cooking`, `resting`. Każde pole jest zakresem minut albo `null`. |
| `ingredients` | Lista składników. |
| `steps` | Uporządkowana lista kroków. |
| `conflicts` | Lista nierozstrzygniętych sprzeczności; pusta, jeśli ich nie ma. |

Wszystkie wymienione pola są obecne, także przy wartościach `null` i pustych listach. `null` oznacza niewiadomą; nie zastępujemy go zerem, pustym tekstem ani napisem „nie podano”. Jeśli źródło podaje niejednoznaczną liczbę porcji, zachowujemy odpowiedni fragment źródła jako kandydatury konfliktu, a `servings` pozostaje `null`.

## Czasy

Jednostką są zawsze minuty, z możliwością liczby dziesiętnej. Odczyt jawnego „pół godziny” jako 30 minut jest dozwolony; wyliczenie niepodanego czasu z sumy etapów nie jest.

```json
{
  "total": null,
  "preparation": { "minMinutes": 10, "maxMinutes": 10 },
  "cooking": { "minMinutes": 25, "maxMinutes": 35 },
  "resting": { "minMinutes": 5, "maxMinutes": 5 }
}
```

Czas dokładny ma równe granice. Zakres zachowuje obie. Wartości są nieujemne, `maxMinutes >= minMinutes`. Brak osobnego czasu gotowania to `null`, a nie 0. Czas całkowity pozostaje niezależny od czasów etapów, które mogą się nakładać. Przy konflikcie danego czasu jego wartość efektywna jest `null`, a obie wersje trafiają do `conflicts`.

Doprecyzowanie po pierwszej próbie: dla jawnej samej górnej granicy „maksymalnie 10 minut” używamy `minMinutes=0, maxMinutes=10` i pokazujemy człowiekowi „do 10 min”. Zero jest tu techniczną granicą nieujemnego czasu, nie dodatkową obserwacją ze źródła. Ten prosty typ nie odróżnia sam w sobie takiej granicy od jawnego zakresu 0–10; oryginalne źródło trzeba zachować. Jawny czas tostowania, pieczenia czy smażenia trafia do `cooking` i pozostaje też w tekście kroku.

## Składnik i ilość

```json
{
  "id": "i1",
  "name": "ciecierzyca",
  "amount": {
    "kind": "exact",
    "value": 240,
    "min": null,
    "max": null,
    "unit": "g",
    "text": null
  },
  "optional": false,
  "qualifiers": ["po odsączeniu"],
  "sourceText": "240 g ciecierzycy po odsączeniu"
}
```

- `id`: unikalny lokalny identyfikator, np. `i1`. Pierwszy odczyt może go nadać; aplikacja później zachowuje go przy poprawkach. Nie identyfikujemy składnika wyłącznie jego pozycją na liście.
- `name`: nazwa składnika albo `null`, np. przy nieczytelnym fragmencie obrazu. Nie zgadujemy nazwy.
- `optional`: informacja pochodząca ze źródła, nie ocena modelu, czy składnik jest kulinarnie potrzebny.
- `qualifiers`: opisowe warunki, np. „ugotowana”, „po odsączeniu”, „do smażenia”. Dzięki temu nie gubimy znaczenia masy.
- `sourceText`: oryginalny fragment tekstu albo transkrypcja czytelnego fragmentu obrazu. To dowód odczytu, nie bieżąca instrukcja działania. Nieczytelnej ilości nie uzupełniamy z własnej wiedzy.

`amount` ma zawsze te same sześć pól. `kind` określa ich interpretację:

| kind | value | min / max | unit | text |
|---|---|---|---|---|
| `exact` | Dodatnia liczba, np. 1.5 | Oba `null` | Jednostka albo `null`, jeśli jej nie podano | `null` |
| `range` | `null` | Dodatnie granice, min ≤ max | Jednostka albo `null` | `null` |
| `text` | `null` | Oba `null` | `null` | Ilość opisowa, np. „do smaku” |
| `unknown` | `null` | Oba `null` | Można zachować czytelną jednostkę | `null` |

Przykłady:

```json
{"kind":"exact","value":0.5,"min":null,"max":null,"unit":"szklanka","text":null}
```

```json
{"kind":"range","value":null,"min":120,"max":150,"unit":"g","text":null}
```

```json
{"kind":"text","value":null,"min":null,"max":null,"unit":null,"text":"do smaku"}
```

```json
{"kind":"unknown","value":null,"min":null,"max":null,"unit":"g","text":null}
```

Nie przeliczamy szklanki na ml ani łyżki na gramy przy pomocy modelu. Dla liczb C# używa `decimal`, a JSON kropki dziesiętnej. Jednostki mają słownik, początkowo np. `g`, `kg`, `ml`, `l`, `szt.`, `łyżka`, `łyżeczka`, `szklanka`, `ząbek`, `kromka`. Nie zastępujemy nieznanej jednostki zgadniętą. Ilość dokładna bez jednostki może być poprawnym odczytem niepełnego źródła, ale wymaga uzupełnienia.

Walidacja v2 obsługuje także `plaster`, `plastry`, `plastrów`; zachowuje tekst jednostki bez przeliczania na gramy. To rozszerzenie słownika aplikacji, bez zmiany schematu dokumentu. Inne nierozpoznane jednostki nadal kieruje do przeglądu.

Walidacja v3 uzupełnia podstawowe odmiany pozostałych znanych jednostek liczonych: `kromka/kromki/kromek`, `łyżka/łyżki/łyżek`, `łyżeczka/łyżeczki/łyżeczek`, `szklanka/szklanki/szklanek`, `ząbek/ząbki/ząbków`, `sztuka/sztuki/sztuk` i skróty `szt.`, `szt`. Nie normalizuje tekstu odpowiedzi ani liczb. Wersję walidatora zapisujemy oddzielnie od wersji instrukcji i schematu, żeby nie mieszać dawnych wyników z późniejszą interpretacją tych samych danych.

`text` nie jest sposobem ukrycia braków: „nie wiem”, „brak ilości” i „nieczytelne” oznaczają `unknown`. Na start aplikacja uznaje „do smaku” za kompletną ilość opisową; inne opisy trzeba jawnie obsłużyć lub przedstawić do wyjaśnienia.

## Kroki i powiązanie ze składnikami

```json
{
  "id": "s1",
  "text": "Przełóż odsączoną fasolę do miski.",
  "ingredientIds": ["i1"],
  "sourceText": "Przełóż 300 g odsączonej fasoli do miski."
}
```

- `text` jest bieżącą instrukcją do gotowania. Jeśli ilość oznacza całą ilość składnika, nie kopiujemy jej liczby do tego tekstu: aktualna ilość pochodzi z `ingredients`.
- `ingredientIds` wskazuje użyte składniki. Wszystkie identyfikatory muszą istnieć w szkicu. Nie muszą powtarzać kolejności składników na liście.
- `sourceText` pozostaje niezmienionym śladem źródła. Po poprawce użytkowniczki może nadal zawierać 300 g, ale nie wyświetlamy go jako aktualnej instrukcji gotowania.
- Prezentacja może pokazać instrukcję oraz powiązany składnik z bieżącą ilością, np. „Przełóż odsączoną fasolę do miski. Składniki w tym kroku: fasola — 250 g”. Nie potrzeba języka szablonów w tekście.

Względne instrukcje typu „dodaj połowę mąki” zachowujemy. Niezależne ilości etapowe, np. „zacznij od 120 g, dosyp do 150 g łącznie”, też pozostają, bo nie są zbędną kopią jednej ilości całkowitej. Po zmianie ilości takiego składnika aplikacja musi oznaczyć powiązane kroki do przeglądu lub zlecić ich ponowną, sprawdzaną aktualizację. Same `ingredientIds` nie rozwiązują dowolnego podziału składnika na etapy. Nie wykonujemy globalnej zamiany liczb w tekście, która mogłaby zmienić inny składnik, temperaturę lub czas.

To rozwiązuje konkretny problem FT-005: jedna aktualna ilość całkowita nie ma dwóch niezależnych kopii w składnikach i instrukcji. Nie gwarantuje automatycznej poprawności wszystkich bardziej złożonych przepisów.

## Konflikty

Dla innego przykładu, w którym `i1` to fasola:

```json
{
  "id": "c1",
  "target": { "type": "ingredientAmount", "id": "i1" },
  "candidates": [
    { "valueText": "250 g", "sourceText": "250 g białej fasoli po odsączeniu" },
    { "valueText": "300 g", "sourceText": "Przełóż 300 g odsączonej fasoli do miski." }
  ]
}
```

- `target.type`: `ingredientAmount`, `servings`, `totalTime`, `preparationTime`, `cookingTime` albo `restingTime`.
- `target.id`: identyfikator składnika przy `ingredientAmount`; `null` dla pozostałych typów.
- `candidates`: co najmniej dwie rzeczywiście różne wersje. `valueText` służy do prezentacji pytania, `sourceText` do wskazania dowodu. Nie wykonujemy kodu ani poleceń z tych tekstów.
- Efektywna ilość składnika jest `unknown`, porcje lub odpowiedni czas są `null`, dopóki użytkowniczka nie rozstrzygnie konfliktu. Nie wybieramy jednej wersji i nie zamieniamy dwóch sprzecznych wartości w zakres.
- Po rozstrzygnięciu aplikacja uzupełnia wartość, usuwa konflikt z listy nierozstrzygniętych i zapisuje zdarzenie w swoim dzienniku. Nie usuwa dowodów z oryginalnego źródła.

Celowo nie wprowadzamy dowolnych ścieżek JSON ani dowolnego kodu operacji. Typ celu ma zamkniętą listę, którą kod C# umie sprawdzić. `valueText` jest tekstem do pokazania, nie automatycznie zaufaną wartością liczbową. Przy odpowiedzi użytkowniczki nowa ilość ma przejść parsowanie i walidację właściwego typu.

## Stan aplikacji poza odpowiedzią modelu

Aplikacja otacza odczytany dokument własnym stanem: identyfikator importu, typ i oryginał źródła, rewizja szkicu, historia zmian, kroki wymagające przeglądu i stan zgody. S1 i S3 mają ten sam kontrakt przepisu; różni się wejściowy dokument przechowywany przez aplikację.

Nie prosimy modelu o pola `approved`, `saved`, `ready`, `missingFields` ani listę pytań. Aplikacja wylicza te informacje na podstawie danych. Proponowana kolejność jej stanów:

1. `needs_clarification`: brak wymaganych danych, nierozwiązany konflikt lub krok wymagający przeglądu.
2. `ready_for_review`: wymagane dane kompletne i walidacja przechodzi, oczekujemy decyzji użytkowniczki.
3. `approved`: jawna zgoda na konkretną rewizję szkicu.

Każda zmiana po zgodzie wymaga ponownego przeglądu. Faktyczny zapis jest osobną operacją i tylko jej wynik może potwierdzić zapis. Rozpoznanie w rozmowie, że użytkowniczka wyraziła zgodę, też wymaga reguł aplikacji; sam tekst modelu nie nadaje tego stanu.

Na początek obowiązkowe są tytuł, liczba porcji, czas całkowity, składniki z nazwami i ilościami/jednostkami oraz kroki. Jawnie opcjonalny składnik bez ilości nie blokuje przepisu; obowiązkowy z nieznaną ilością blokuje. Osobne czasy etapów mogą być `null`. Ilość „do smaku” nie wymaga liczby. Pusta lista kroków lub składników oznacza niekompletny szkic, a nie poprawny import.

## Stan implementacji i dalsza integracja

Prototyp zawiera klasy C#, JSON Schema, nową instrukcję, walidację i `ExtractRecipeAsync(text)`. Zwraca `ExtractionResult`, obejmujący dokument, wynik walidacji i czas. To nowy wariant, oceniany oddzielnie od swobodnej rozmowy. Dalsza integracja obejmie poprawki na identyfikatorach zachowanych przez aplikację i stan rozmowy; nie ponowne nadpisywanie całego szkicu przez model przy każdej turze.
