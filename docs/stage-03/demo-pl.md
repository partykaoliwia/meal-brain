# Demo Etapu 3: tool calling na szkicu przepisu

Co pokazujemy: model (gemma4:12b przez Ollamę) nie zwraca już JSON-a z propozycją zmian,
tylko wywołuje narzędzia (wzorzec z lab 04: Microsoft.Extensions.AI + UseFunctionInvocation).
Narzędzia wykonuje aplikacja przez `DraftSession`, a błędy walidacji wracają do modelu jako wynik narzędzia.

Narzędzia: `get_draft`, `set_ingredient_amount`, `add_ingredient`, `remove_ingredient`, `set_servings`, `set_time`, `save_recipe`.
`save_recipe` nie zapisuje nic samo: aplikacja pyta w konsoli „Czy zapisać rewizję N? [t/N]”.

## Uruchomienie

```powershell
ollama serve
```

```powershell
dotnet run --project src/MealBrain.ModelLab -- --tools gemma4:12b
```

Pierwsza odpowiedź trwa dłużej (ładowanie modelu).

## Scenariusz (przepis „ogórki”, brakuje porcji i ilości jogurtu)

1. `/case ogorki` — pokaż źródło i szkic: jogurt ma `BRAK ILOŚCI`, brak porcji.
2. `Jogurtu daj tyle co zwykle` — **przypadek błędu**: widać `[tool] set_ingredient_amount -> BŁĄD: ilość opisowa
   „tyle co zwykle” jest niedozwolona...`. Model dostaje błąd i dopytuje o konkretną ilość. Szkic się nie zmienił (`/show`).
3. `Jogurtu 150 g` — `[tool] set_ingredient_amount -> OK`, rewizja rośnie.
4. `Dodaj sól, 1 łyżeczka` — `add_ingredient`. Potem `Usuń sól` — `remove_ingredient`.
5. `Ustaw czas całkowity na 10 minut` — `set_time`.
6. `Zapisz przepis` zanim są porcje — błąd `save_recipe` (stan NeedsClarification), model tłumaczy czego brakuje.
7. `Ma być 2 porcje` — `set_servings`. Znów `Zapisz przepis` — aplikacja pyta `Czy zapisać rewizję N? [t/N]`,
   odpowiedz `t` — powstaje `runs/.../saved-recipe.json`.
8. `/history` — pełna historia zmian z wypowiedziami użytkowniczki.

## Co powiedzieć

- Model tylko wybiera narzędzie, odpowiada za nie aplikacja (zasada z lab 04).
- Walidator jest po stronie aplikacji, więc model nie wymyśli ilości ani nie zatwierdzi przepisu bez zgody.
- Zgoda na zapis jest pytaniem aplikacji do użytkowniczki, nie decyzją modelu.

## Uwaga techniczna

Model 12B bywa niedeterministyczny w doborze narzędzi; jeśli w kroku 2 odpowie tekstem zamiast wywołać narzędzie,
powtórz wypowiedź albo użyj `Ustaw jogurt na tyle co zwykle`.
