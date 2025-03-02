using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KontrolaNawykow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace KontrolaNawykow.Pages.Account
{
    public class RegisterDietModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterDietModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public double Bmi { get; set; }

        [BindProperty]
        public string BmiCategory { get; set; }

        [BindProperty]
        public int CaloriesDeficit { get; set; }

        [BindProperty]
        public int ProteinGrams { get; set; }

        [BindProperty]
        public int CarbsGrams { get; set; }

        [BindProperty]
        public int FatGrams { get; set; }

        public int ProteinPercentage => CaloriesDeficit > 0 ? (int)Math.Round(ProteinGrams * 4.0 / CaloriesDeficit * 100) : 0;
        public int CarbsPercentage => CaloriesDeficit > 0 ? (int)Math.Round(CarbsGrams * 4.0 / CaloriesDeficit * 100) : 0;
        public int FatPercentage => CaloriesDeficit > 0 ? (int)Math.Round(FatGrams * 9.0 / CaloriesDeficit * 100) : 0;

        [BindProperty]
        public bool UseCustomValues { get; set; }

        [BindProperty]
        public double CustomBmi { get; set; }

        [BindProperty]
        public int CustomCaloriesDeficit { get; set; }

        [BindProperty]
        public int CustomProteinGrams { get; set; }

        [BindProperty]
        public int CustomCarbsGrams { get; set; }

        [BindProperty]
        public int CustomFatGrams { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Pobierz ID użytkownika z TempData
                if (TempData["UserId"] == null)
                {
                    ErrorMessage = "Brak danych użytkownika. Rozpocznij rejestrację od początku.";
                    return RedirectToPage("/Account/Register");
                }

                int userId = (int)TempData["UserId"];
                TempData.Keep("UserId");

                // Pobierz dane użytkownika
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    ErrorMessage = "Nie znaleziono użytkownika.";
                    return RedirectToPage("/Account/Register");
                }

                // Oblicz BMI jeśli użytkownik ma podaną wagę i wzrost
                if (user.Waga.HasValue && user.Wzrost.HasValue)
                {
                    // BMI = waga[kg] / (wzrost[m])²
                    double wzrostWMetrach = user.Wzrost.Value / 100.0;
                    Bmi = user.Waga.Value / (wzrostWMetrach * wzrostWMetrach);

                    // Ustaw kategorię BMI
                    BmiCategory = GetBmiCategory(Bmi);

                    // Oblicz deficyt kaloryczny na podstawie danych użytkownika
                    CaloriesDeficit = CalculateCaloriesDeficit(user);

                    // Oblicz rozkład makroskładników
                    CalculateMacronutrients(user);
                }
                else
                {
                    // Domyślne wartości jeśli dane są niekompletne
                    Bmi = 0;
                    BmiCategory = "Brak danych";
                    CaloriesDeficit = 0;
                    ProteinGrams = 0;
                    CarbsGrams = 0;
                    FatGrams = 0;
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Wystąpił błąd podczas przetwarzania danych: " + ex.Message;
                return RedirectToPage("/Account/Register");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Pobierz ID użytkownika z TempData
                if (TempData["UserId"] == null)
                {
                    ErrorMessage = "Brak danych użytkownika. Rozpocznij rejestrację od początku.";
                    return RedirectToPage("/Account/Register");
                }

                int userId = (int)TempData["UserId"];

                // Pobierz dane użytkownika
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    ErrorMessage = "Nie znaleziono użytkownika.";
                    return RedirectToPage("/Account/Register");
                }

                // Dodaj nowe pola do modelu User, jeśli ich jeszcze nie ma
                if (UseCustomValues)
                {
                    // Używamy wartości wprowadzonych przez użytkownika
                    user.CustomBmi = CustomBmi;
                    user.CustomCaloriesDeficit = CustomCaloriesDeficit;
                    user.CustomProteinGrams = CustomProteinGrams;
                    user.CustomCarbsGrams = CustomCarbsGrams;
                    user.CustomFatGrams = CustomFatGrams;
                }
                else
                {
                    // Oblicz i zapisz standardowe wartości
                    double wzrostWMetrach = user.Wzrost.Value / 100.0;
                    double bmi = user.Waga.Value / (wzrostWMetrach * wzrostWMetrach);
                    int caloriesDeficit = CalculateCaloriesDeficit(user);

                    user.CustomBmi = bmi;
                    user.CustomCaloriesDeficit = caloriesDeficit;

                    // Oblicz i zapisz standardowy rozkład makroskładników
                    CalculateMacronutrients(user);
                    user.CustomProteinGrams = ProteinGrams;
                    user.CustomCarbsGrams = CarbsGrams;
                    user.CustomFatGrams = FatGrams;
                }

                // Zapisz zmiany w bazie danych
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Automatyczne logowanie użytkownika
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Przekierowanie na stronę główną
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Wystąpił błąd podczas przetwarzania danych: " + ex.Message;
                TempData.Keep("UserId");
                return Page();
            }
        }

        private string GetBmiCategory(double bmi)
        {
            if (bmi < 16)
                return "Wygłodzenie";
            else if (bmi < 17)
                return "Wychudzenie";
            else if (bmi < 18.5)
                return "Niedowaga";
            else if (bmi < 25)
                return "Waga prawidłowa";
            else if (bmi < 30)
                return "Nadwaga";
            else if (bmi < 35)
                return "Otyłość I stopnia";
            else if (bmi < 40)
                return "Otyłość II stopnia";
            else
                return "Otyłość III stopnia";
        }

        private int CalculateCaloriesDeficit(User user)
        {
            // Sprawdzenie czy mamy wszystkie niezbędne dane
            if (!user.Waga.HasValue || !user.Wzrost.HasValue || !user.Wiek.HasValue)
            {
                return 2000; // Wartość domyślna jeśli brakuje danych
            }

            // Podstawowe zapotrzebowanie kaloryczne (BMR) - wzór Mifflina-St Jeora
            double bmr;

            // BMR w zależności od płci
            if (user.Plec == Gender.Mezczyzna)
            {
                // Dla mężczyzn: BMR = 10 * waga + 6.25 * wzrost - 5 * wiek + 5
                bmr = 10 * user.Waga.Value + 6.25 * user.Wzrost.Value - 5 * user.Wiek.Value + 5;
            }
            else
            {
                // Dla kobiet: BMR = 10 * waga + 6.25 * wzrost - 5 * wiek - 161
                bmr = 10 * user.Waga.Value + 6.25 * user.Wzrost.Value - 5 * user.Wiek.Value - 161;
            }

            // Współczynnik aktywności fizycznej (PAL)
            double pal = 1.2; // Domyślnie siedzący tryb życia

            // Uwzględnienie aktywności fizycznej
            if (user.AktywnoscFizyczna != null)
            {
                if (user.AktywnoscFizyczna.Contains("0 treningów"))
                    pal = 1.2; // Siedzący tryb życia
                else if (user.AktywnoscFizyczna.Contains("1-3"))
                    pal = 1.375; // Lekka aktywność
                else if (user.AktywnoscFizyczna.Contains("4-5"))
                    pal = 1.55; // Umiarkowana aktywność
            }

            // Uwzględnienie rodzaju pracy
            if (user.RodzajPracy != null)
            {
                if (user.RodzajPracy == "Fizyczna")
                    pal += 0.1; // Dodatkowa aktywność dla pracy fizycznej
                else if (user.RodzajPracy == "Pół na pół")
                    pal += 0.05; // Dodatkowa aktywność dla pracy mieszanej
            }

            // Całkowite dzienne zapotrzebowanie energetyczne (TDEE)
            double tdee = bmr * pal;

            // Uwzględnienie wieku (metabolizm spada z wiekiem)
            if (user.Wiek > 40)
                tdee *= 0.98; // Lekka redukcja dla osób powyżej 40 lat
            if (user.Wiek > 60)
                tdee *= 0.97; // Dalsza redukcja dla osób powyżej 60 lat

            // Deficyt kaloryczny zależny od celu
            int deficit = 0;

            if (user.Cel == UserGoal.Schudniecie)
            {
                // Deficyt dla schudnięcia (około 20% TDEE)
                deficit = (int)(tdee * 0.8);
            }
            else if (user.Cel == UserGoal.PrzybranieMasy)
            {
                // Nadwyżka dla przybrania masy (około 10% TDEE)
                deficit = (int)(tdee * 1.1);
            }
            else // UserGoal.ZdroweNawyki
            {
                // Utrzymanie wagi
                deficit = (int)tdee;
            }

            // Minimalna wartość kalorii zależna od płci
            int minCalories = user.Plec == Gender.Mezczyzna ? 1500 : 1200;

            // Upewnij się, że deficyt nie jest zbyt niski
            if (deficit < minCalories)
                deficit = minCalories;

            return deficit;
        }

        private void CalculateMacronutrients(User user)
        {
            // Jeśli nie mamy danych o kaloriach, nie możemy obliczyć makroskładników
            if (CaloriesDeficit <= 0)
            {
                ProteinGrams = 0;
                CarbsGrams = 0;
                FatGrams = 0;
                return;
            }

            // Obliczanie makroskładników w zależności od celu i płci
            if (user.Cel == UserGoal.Schudniecie)
            {
                // Wyższe białko, niższe węglowodany, umiarkowane tłuszcze dla redukcji
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 2.0 : 1.8;
                ProteinGrams = (int)(user.Waga.Value * proteinMultiplier); // Więcej białka na kg masy ciała
                FatGrams = (int)(CaloriesDeficit * 0.25 / 9); // 25% kalorii z tłuszczów
                CarbsGrams = (int)((CaloriesDeficit - (ProteinGrams * 4) - (FatGrams * 9)) / 4);
            }
            else if (user.Cel == UserGoal.PrzybranieMasy)
            {
                // Wysokie białko, wysokie węglowodany, umiarkowane tłuszcze dla zwiększenia masy
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 1.8 : 1.6;
                ProteinGrams = (int)(user.Waga.Value * proteinMultiplier);
                FatGrams = (int)(CaloriesDeficit * 0.25 / 9); // 25% kalorii z tłuszczów
                CarbsGrams = (int)((CaloriesDeficit - (ProteinGrams * 4) - (FatGrams * 9)) / 4);
            }
            else // UserGoal.ZdroweNawyki
            {
                // Zrównoważony stosunek makroskładników dla zdrowych nawyków
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 1.6 : 1.4;
                ProteinGrams = (int)(user.Waga.Value * proteinMultiplier);
                FatGrams = (int)(CaloriesDeficit * 0.3 / 9); // 30% kalorii z tłuszczów
                CarbsGrams = (int)((CaloriesDeficit - (ProteinGrams * 4) - (FatGrams * 9)) / 4);
            }

            // Uwzględnienie aktywności fizycznej przy obliczaniu węglowodanów
            if (user.AktywnoscFizyczna != null && user.AktywnoscFizyczna.Contains("4-5"))
            {
                // Więcej węglowodanów dla osób aktywnych fizycznie
                int extraCarbs = (int)(user.Waga.Value * 0.5); // Dodatkowe 0.5g węglowodanów na kg masy ciała
                CarbsGrams += extraCarbs;

                // Dostosowanie kalorii
                CaloriesDeficit += extraCarbs * 4;
            }

            // Upewnij się, że wartości nie są ujemne
            if (CarbsGrams < 50) CarbsGrams = 50; // Minimalna wartość węglowodanów
            if (ProteinGrams < 40) ProteinGrams = 40; // Minimalna wartość białka
            if (FatGrams < 20) FatGrams = 20; // Minimalna wartość tłuszczów

            // Korekta kaloryczności po dostosowaniu makroskładników
            CaloriesDeficit = (ProteinGrams * 4) + (CarbsGrams * 4) + (FatGrams * 9);
        }
    }
}