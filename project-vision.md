# Wizja projektu: MealBrAIn

## 1. Problem

Planowanie posiłków, gromadzenie przepisów i organizacja zakupów przy napiętym grafiku wiążą się z kilkoma problemami:

- **Rozproszenie przepisów** - inspiracje pochodzą z nieustrukturyzowanych źródeł (zrzuty ekranu, linki do blogów, opisy z social mediów), więc trudno zebrać je w jedną bazę, którą da się przeszukać.
- **Niekompletność opisów** - przepisy z postów często nie mają dokładnych ilości ani liczby porcji, co utrudnia gotowanie i zakupy.
- **Niedopasowanie do planu tygodnia** - czasochłonne gotowanie zaplanowane na dzień pełen zajęć kończy się porzuceniem planu i zmarnowanymi składnikami.
- **Ręczne tworzenie listy zakupów** - składniki z różnych przepisów trzeba samodzielnie zebrać, policzyć i przeliczyć na wspólne jednostki, przez co łatwo coś pominąć lub kupić w złej ilości.

**Efekt:** zapisane przepisy się marnują, plan posiłków rzadko się udaje, a zakupy są nietrafione.

## 2. Użytkownicy

- **Studenci i osoby zapracowane** z nieregularnym planem tygodnia, którzy chcą dopasować gotowanie do kalendarza.
- **Osoby gotujące w domu**, które chcą budować własną bazę przepisów ze zdjęć, linków i notatek.
- **Osoby szukające szybkich inspiracji** na podstawie składników pod ręką i dostępnego czasu.

## 3. Scenariusze użycia

| # | Scenariusz | Opis działania |
|---|---|---|
| **S1** | **Import ze zdjęcia / screenshota** | Agent wyciąga przepis: składniki z ilościami, kroki, porcje, czas, tagi (np. wysokobiałkowe, wegetariańskie) → użytkownik zatwierdza → zapis do bazy. |
| **S2** | **Import z linku** | Jeśli strona zapisuje przepis w standardowym, uporządkowanym formacie, system odczytuje go bezpośrednio. Jeśli nie, agent sam wyciąga przepis z treści strony, tak jak w S1. |
| **S3** | **Import z opisu** | Agent strukturyzuje wklejony tekst jak w S1; braki (ilości, porcje) oznacza i dopytuje o nie przed zapisem. |
| **S4** | **Szukanie w bazie** | „Co zrobię z kurczakiem i ryżem w 20 min?” → agent przeszukuje bazę (RAG) i proponuje dania. |
| **S5** | **Szukanie w internecie** | Gdy baza nie wystarcza lub użytkownik prosi o „coś nowego” → agent proponuje kilka przepisów z linkiem do źródła → po akceptacji import jak w S2. |
| **S6** | **Plan tygodnia z kalendarzem** | Zajęty dzień → szybkie danie, ugotowanie dzień wcześniej porcji na 2 dni lub jedzenie na mieście; wolny dzień → coś dłuższego; wyjście wieczorem → bez posiłku w domu. |
| **S7** | **Lista zakupów** | Zsumowane składniki z planu, ujednolicone jednostki, podział na działy sklepu. |

## 4. Rola agenta AI

Problem wymaga schematu **cel → decyzja w locie → działanie**, a nie sztywnych reguł i formularzy. Agent:

- **strukturyzuje nieustrukturyzowane dane** - odczytuje przepis ze zdjęcia, strony lub opisu, wykrywa braki i dopytuje,
- **rozumie kontekst życiowy** - przekłada plan dnia (np. zajęcia do 20:00) na decyzje,
- **sam planuje kroki** - decyduje, kiedy przeszukać bazę (RAG), sięgnąć do internetu, pobrać kalendarz (MCP) lub wywołać narzędzia (tool calling).

**AI tam, gdzie dane są nieustrukturyzowane; kod tam, gdzie wynik musi być dokładny:**

| AI | Kod |
|---|---|
| odczytanie przepisu ze zdjęcia, opisu lub strony | odczytanie przepisu, który strona podaje w gotowej, uporządkowanej formie |
| ujednolicenie składników („2 ząbki czosnku” → czosnek, 2 szt.) | sumowanie ilości i przeliczanie jednostek |
| zrozumienie prośby użytkownika i ocena, jak zajęty jest dany dzień | odczyt kalendarza, zapis planu, tworzenie listy zakupów |
| ocena, czy przepisy w bazie wystarczą i czy te z internetu pasują | wyszukiwanie w internecie i w bazie przepisów |

### Mechanizmy

* **Model:** musi umieć korzystać z narzędzi i odczytywać obrazy; konkretny wybierzemy na etapie implementacji.
* **Tool calling:** narzędzia do importu przepisów (ze zdjęcia, tekstu lub strony) i zapisu ich w bazie; dokładny zestaw ustalimy podczas implementacji.
* **MCP:** agent korzysta z własnego serwera „Planer”, który odpowiada za kalendarz (plik `.ics`), plan tygodnia i listę zakupów, oraz z gotowych serwerów do wyszukiwania i pobierania stron internetowych. Dzięki temu planowanie działa jako osobna usługa, a integracji z internetem nie musimy pisać od zera.
* **RAG:** baza przepisów (wstępny zbiór oraz przepisy dodane przez użytkownika), przeszukiwana po znaczeniu, a nie tylko po słowach.
* **Interfejs:** konwersacyjny (konsola lub minimalny czat).

## 5. Zakres

### W zakresie (MVP)

| Obszar | Co dokładnie |
|---|---|
| Import (S1–S3) | zdjęcie, link, tekst; dopytywanie o braki; zatwierdzenie przed zapisem |
| Baza (S4) | wstępny zbiór przepisów; wyszukiwanie po znaczeniu + filtry (składnik, czas, tagi) |
| Internet (S5) | kilka propozycji ze źródłem; zapis tylko po akceptacji; ochrona przed ukrytymi poleceniami na stronach (prompt injection) |
| Plan tygodnia (S6) | kalendarz `.ics` przez MCP; jeden główny posiłek dziennie |
| Lista zakupów (S7) | suma składników, słownik jednostek (g, kg, ml, l, szt., łyżka, łyżeczka, szklanka), działy sklepu |
| Ewaluacja | ~20 przypadków importu porównanych z ręcznie przygotowanym poprawnym wynikiem; trafność wyszukiwania; zgodność planu z kalendarzem; poprawność listy zakupów (test automatyczny); przypadki trudne: brak ilości, nieczytelne zdjęcie, strona bez przepisu, prompt injection, pusty / przepełniony kalendarz |

### Rozszerzenia

- bezpośrednia integracja z Google Calendar,
- zapis posiłków do kalendarza (po akceptacji),
- prosta aplikacja webowa.

### Poza zakresem

- stałe śledzenie stanu lodówki (składniki podawane doraźnie w rozmowie),
- liczenie kalorii i makroskładników (tagi tylko orientacyjne),
- filmiki i automatyczne pobieranie treści z social mediów,
- zamawianie zakupów online,
- aplikacja mobilna, wielu użytkowników.
