# MealBrAIn

**MealBrAIn** to inteligentny asystent kulinarny oparty na agentach AI (realizowany na platformie .NET), który pomaga organizować codzienne gotowanie, redukować marnowanie żywności i planować zakupy.

Projekt łączy nowoczesne mechanizmy AI z realnymi potrzebami domowymi:
- **Importuje przepisy** z dowolnych źródeł (zdjęcia, linki internetowe, opisy tekstowe).
- **Planuje posiłki** w oparciu o Twój kalendarz (np. dopasowując *meal prep* do zajętych dni i wolnych weekendów).
- **Wyszukuje inspiracje** w lokalnej bazie wiedzy (RAG) dopasowane do tego, co masz pod ręką i na co masz ochotę.
- **Generuje sprytną listę zakupów** – zsumowaną, z ujednoliconymi jednostkami i pogrupowaną działami sklepu.

Projekt jest rozwijany stopniowo wraz z kolejnymi etapami zajęć i realizacji.

## Dokumentacja

- [Wizja projektu](project-vision.md)
- [Propozycja wspólnego formatu szkicu przepisu — S1 i S3](docs/contracts/recipe-draft-v1.md)
- [S3 — podsumowanie pracy Oliwii na etap 2](docs/stage-02/s3-stage-02-summary.md)
- [Etap 2 — przekazanie dla Marii i Patrycji, przykłady i uruchomienie](docs/stage-02/team-handoff.md)
- [S3 — końcowy wynik tej serii: 5/6, czasy poprawione, otwarty problem cytatu](docs/stage-02/reports/2026-11-04-s3-extraction-check-v2/report.md)
- [S3 — protokół zakończonej regresji sześciu odczytów](docs/stage-02/s3-extraction-check-v2.md)
- [S3 — wynik szerszej próby ekstrakcji: 2/6](docs/stage-02/reports/2026-11-04-s3-extraction-check-v1/report.md)
- [S3 — wynik poprawionej instrukcji: konflikt i czas tostowania](docs/stage-02/reports/2026-11-04-s3-extraction-v2/report.md)
- [S3 — raport pierwszych trzech odczytów do JSON](docs/stage-02/reports/2026-11-04-s3-extraction-v1/report.md)
- [Etap 2 — instrukcja uruchomienia testów w C# i Ollamie](docs/stage-02/how-to-test.md)
- [Etap 2 — sześć przypadków testowych importu przepisu](docs/stage-02/test-cases.md)
- [Etap 2 — szablon wyników testów](docs/stage-02/results-template.md)
- [Etap 2 — porównanie modeli dla S3](docs/stage-02/model-comparison.md)
- [Etap 2 — następne kroki S3](docs/stage-02/s3-next-steps.md)
- [Etap 2 — automatyczna próba finalistów: 12 nowych scenariuszy i kryteria wyboru](docs/stage-02/finalists-v1.md)
- [Etap 2 — wyniki próby finalistów](docs/stage-02/reports/2026-11-04-finalists-v1/report.md)

## Struktura

```text
meal-brain/
├── README.md
├── project-vision.md
├── src/
│   └── MealBrain.ModelLab/  # konsola C#, instrukcja i logowanie rozmów
├── evals/
│   └── finalists-v1/        # nowe wejścia i ustalone wiadomości
├── tests/
│   └── MealBrain.ModelLab.Checks/  # sprawdzenie mechaniki testów bez modelu
└── docs/
    ├── contracts/          # propozycja formatu RecipeDraft i przykład JSON
    └── stage-02/
        ├── how-to-test.md
        ├── finalists-v1.md
        ├── model-comparison.md
        ├── s3-next-steps.md
        ├── test-cases.md
        ├── results-template.md
        ├── prompt-experiment-v2.md
        ├── prompts/
        └── reports/
```
## Autorzy

- Patrycja Borkowska
- Maria Deperas
- Oliwia Partyka

Grupa: 2
  
## Licencja

Kod źródłowy tego projektu jest udostępniany na licencji MIT.
