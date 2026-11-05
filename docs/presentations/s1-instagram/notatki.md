# Notatki do wystąpienia, około 5–7 minut

## 1. Import przepisu ze screena — około 25 sekund

W MealBrAIn odpowiadam za scenariusz S1, czyli import przepisu z obrazu. Chcemy odczytać dane widoczne na screenie, a brakujące informacje pozostawić jako niewiadome. Wynik ma ten sam format RecipeDraft co moduł importu tekstu, żeby później można było przejść do rozmowy i uzupełniania przepisu.

## 2. Przebieg testu — około 60 sekund

Zestaw obejmuje sześć obrazów i dwa lokalne modele w Ollamie: Gemmę 3 4B oraz Qwen3-VL 4B Instruct. Łącznie wykonaliśmy dwanaście odczytów. Przed uruchomieniem zapisaliśmy oczekiwane dane, braki i kryteria, a następnie zamroziliśmy pliki i ustawienia przez hashe.

Model dostawał obraz, instrukcję i schemat JSON, bez odpowiedzi wzorcowej. Każdy obraz miał osobne żądanie, bez pamięci innych przepisów. Ustawiliśmy temperaturę 0, kontekst 16384 i limit odpowiedzi 4096 tokenów. Ocenę podzieliliśmy na walidację kodu oraz porównanie danych z obrazem. Nie robiliśmy powtórzeń ani oddzielnej rozgrzewki.

## 3. Prompt S1 — około 35 sekund

Na slajdzie są dosłowne fragmenty instrukcji użytej w obu modelach. Wymagaliśmy wyłącznie JSON zgodnego z RecipeDraft, odczytu informacji widocznych na obrazie oraz jawnych niewiadomych. Prompt zabraniał dopisywania typowych porcji i zatwierdzania przepisu. Pełna instrukcja zawierała także reguły ilości, czasów, cytatów i konfliktów.

Do wiadomości systemowej dołączaliśmy schemat odpowiedzi. Wiadomość użytkownika była taka sama dla wszystkich przypadków i miała załączony konkretny screen. Pełny tekst promptu, schemat i wiadomość użytkownika są dołączone do projektu. To wersja użyta w badaniu, bez poprawek po obejrzeniu wyników.

## 4. Materiał badawczy — około 40 sekund

Wszystkie obrazy są z Instagrama. Pięć zawiera fragmenty przepisów, a szósty pokazuje samą potrawę. Jeden przepis jest po angielsku. Na slajdzie widać cztery polskie przykłady i ich układ, pełne obrazy są w projekcie.

Interesowały nas naturalne problemy takich źródeł: przyprawy wymienione dopiero w krokach, ułamki, różne czasy oraz ilości podane w kilku miejscach. Kaszka ma tylko listę składników i odsyła do filmu, którego model nie otrzymał. Braków nie dopisywaliśmy do screenów. Ten zestaw nie obejmuje jeszcze stron internetowych ani rozmazanych cyfr.

## 5. Batony — około 50 sekund

Screen zawiera listę składników, ale nie zawiera instrukcji wykonania, liczby porcji ani czasu. Przy oleju kokosowym jest jedna czwarta bez jednostki. Oczekujemy zachowania tej liczby i nieznanej jednostki.

Gemma zapisała trzy czwarte jako 0,375 i dopisała cztery kroki, m.in. rozgrzanie piekarnika do 175 stopni. Oba modele dodały do oleju jednostkę cup, choć jej nie ma na screenie. Qwen zachował sam ułamek poprawnie i nie dopisał kroków, ale wynik nadal zawierał błędy kontraktu. To pokazuje potrzebę oddzielenia odczytu od uzupełniania przepisu wiedzą modelu.

## 6. Sam kadr potrawy — około 40 sekund

Na tym obrazie nie ma czytelnej listy składników ani przygotowania. Oceniamy wyłącznie to, co widzi model, bez pobierania reszty posta czy oglądania filmu.

Gemma wymyśliła m.in. 400 g fasoli, szklankę ryżu i kroki gotowania. Qwen poprawnie zwrócił not_recipe i pusty obiekt przepisu. Ten przykład dał mu jedyne pełne zaliczenie w całej próbie. Kod uznał dokument Gemmy za strukturalnie akceptowalny, więc sama walidacja nie wykryła zmyślenia treści.

## 7. JSON i reguły kontraktu — około 45 sekund

Wszystkie odpowiedzi dało się odczytać jako JSON i sparsować do klas C#. To nie wystarczy do poprawnego importu. Dla dokładnej ilości kontrakt wymaga wartości i jednostki, a pola min, max i text mają pozostać null.

Qwen często wypełniał je jednocześnie. Walidator w C# wykrywał takie niespójności, podobnie jak odwołanie do nieistniejącego składnika. Nie porówna jednak samodzielnie tych danych z pikselami. Dlatego osobno oceniamy zgodność ze screenem i wierność cytatów sourceText.

## 8. Wyniki — około 55 sekund

Oba modele zwróciły sześć kompletnych odpowiedzi API. Gemma miała cztery dokumenty zgodne z regułami danych, a Qwen dwa. Pełne zaliczenia wyniosły zero na sześć i jeden na sześć.

Najważniejsze doprecyzowanie: jeden poprawny wynik Qwena to odrzucenie obrazu bez przepisu. Żaden model nie zaimportował poprawnie całego jednego z pięciu rzeczywistych fragmentów przepisów.

Mediany czasu wyniosły około 18,06 s i 17,67 s. Są tylko informacyjne: pierwszy odczyt obejmował ładowanie, a szybkie odpowiedzi Qwena były odrzuceniami. Jedno odrzucenie było błędne, bo model odrzucił także kaszkę z listą składników. Nie wnioskujemy z tego pomiaru o ogólnej przewadze szybkości.

## 9. Wnioski — około 40 sekund

Pierwszy wariant, czyli bezpośredni odczyt obrazu do rozbudowanego RecipeDraft, wymaga dalszych prac. Mamy konkretne dowody błędów, zapisane odpowiedzi oraz wspólny format integracji z resztą projektu.

Kolejny eksperyment może oddzielać wierną transkrypcję obrazu od mapowania tekstu na strukturę przepisu. Możemy również doprecyzować reguły pól ilości w schemacie. Tego wariantu jeszcze nie testowaliśmy. Po poprawkach potrzebne będą nowe obrazy, w tym screeny stron internetowych, żeby ocena nie opierała się wyłącznie na znanych przykładach.
