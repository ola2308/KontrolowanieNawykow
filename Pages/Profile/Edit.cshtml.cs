using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace KontrolaNawykow.Pages.Profile
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public UserEditViewModel UserEdit { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Inicjalizacja modelu z danymi u�ytkownika
            UserEdit = new UserEditViewModel
            {
                Username = user.Username,
                Email = user.Email,
                Wiek = user.Wiek,
                Plec = user.Plec,
                Waga = user.Waga,
                Wzrost = user.Wzrost,
                AktywnoscFizyczna = user.AktywnoscFizyczna,
                RodzajPracy = user.RodzajPracy,
                Cel = user.Cel
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Sprawdzenie unikalno�ci nazwy u�ytkownika i emaila
            if (user.Username != UserEdit.Username)
            {
                var existingUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == UserEdit.Username);
                if (existingUsername != null)
                {
                    ModelState.AddModelError("UserEdit.Username", "Nazwa u�ytkownika jest ju� zaj�ta.");
                    return Page();
                }
            }

            if (user.Email != UserEdit.Email)
            {
                var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == UserEdit.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("UserEdit.Email", "Adres email jest ju� u�ywany.");
                    return Page();
                }
            }

            // Aktualizacja danych u�ytkownika
            user.Username = UserEdit.Username;
            user.Email = UserEdit.Email;
            user.Wiek = UserEdit.Wiek;
            user.Plec = UserEdit.Plec;
            user.Waga = UserEdit.Waga;
            user.Wzrost = UserEdit.Wzrost;
            user.AktywnoscFizyczna = UserEdit.AktywnoscFizyczna;
            user.RodzajPracy = UserEdit.RodzajPracy;
            user.Cel = UserEdit.Cel;

            // Aktualizacja BMI i zalecanego deficytu kalorycznego
            if (user.Wzrost.HasValue && user.Waga.HasValue)
            {
                double heightInMeters = user.Wzrost.Value / 100.0;
                double bmi = user.Waga.Value / (heightInMeters * heightInMeters);
                user.CustomBmi = bmi;

                // Zaktualizuj r�wnie� kalorie i makrosk�adniki
                int caloriesDeficit = CalculateCaloriesDeficit(user);
                user.CustomCaloriesDeficit = caloriesDeficit;

                // Oblicz i zapisz makrosk�adniki
                CalculateMacronutrients(user);
            }

            // Zapisz zmiany
            try
            {
                await _context.SaveChangesAsync();
                SuccessMessage = "Dane profilu zosta�y zaktualizowane.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Wyst�pi� b��d podczas zapisywania zmian: {ex.Message}";
                return Page();
            }

            return RedirectToPage("./Index");
        }

        private int CalculateCaloriesDeficit(User user)
        {
            // Sprawdzenie czy mamy wszystkie niezb�dne dane
            if (!user.Waga.HasValue || !user.Wzrost.HasValue || !user.Wiek.HasValue)
            {
                return 2000; // Warto�� domy�lna je�li brakuje danych
            }

            // Podstawowe zapotrzebowanie kaloryczne (BMR) - wz�r Mifflina-St Jeora
            double bmr;

            // BMR w zale�no�ci od p�ci
            if (user.Plec == Gender.Mezczyzna)
            {
                // Dla m�czyzn: BMR = 10 * waga + 6.25 * wzrost - 5 * wiek + 5
                bmr = 10 * user.Waga.Value + 6.25 * user.Wzrost.Value - 5 * user.Wiek.Value + 5;
            }
            else
            {
                // Dla kobiet: BMR = 10 * waga + 6.25 * wzrost - 5 * wiek - 161
                bmr = 10 * user.Waga.Value + 6.25 * user.Wzrost.Value - 5 * user.Wiek.Value - 161;
            }

            // Wsp�czynnik aktywno�ci fizycznej (PAL)
            double pal = 1.2; // Domy�lnie siedz�cy tryb �ycia

            // Uwzgl�dnienie aktywno�ci fizycznej
            if (user.AktywnoscFizyczna != null)
            {
                if (user.AktywnoscFizyczna.Contains("0 trening�w"))
                    pal = 1.2; // Siedz�cy tryb �ycia
                else if (user.AktywnoscFizyczna.Contains("1-3"))
                    pal = 1.375; // Lekka aktywno��
                else if (user.AktywnoscFizyczna.Contains("4-5"))
                    pal = 1.55; // Umiarkowana aktywno��
            }

            // Uwzgl�dnienie rodzaju pracy
            if (user.RodzajPracy != null)
            {
                if (user.RodzajPracy == "Fizyczna")
                    pal += 0.1; // Dodatkowa aktywno�� dla pracy fizycznej
                else if (user.RodzajPracy == "P� na p�")
                    pal += 0.05; // Dodatkowa aktywno�� dla pracy mieszanej
            }

            // Ca�kowite dzienne zapotrzebowanie energetyczne (TDEE)
            double tdee = bmr * pal;

            // Uwzgl�dnienie wieku (metabolizm spada z wiekiem)
            if (user.Wiek > 40)
                tdee *= 0.98; // Lekka redukcja dla os�b powy�ej 40 lat
            if (user.Wiek > 60)
                tdee *= 0.97; // Dalsza redukcja dla os�b powy�ej 60 lat

            // Deficyt kaloryczny zale�ny od celu
            int deficit = 0;

            if (user.Cel == UserGoal.Schudniecie)
            {
                // Deficyt dla schudni�cia (oko�o 20% TDEE)
                deficit = (int)(tdee * 0.8);
            }
            else if (user.Cel == UserGoal.PrzybranieMasy)
            {
                // Nadwy�ka dla przybrania masy (oko�o 10% TDEE)
                deficit = (int)(tdee * 1.1);
            }
            else // UserGoal.ZdroweNawyki
            {
                // Utrzymanie wagi
                deficit = (int)tdee;
            }

            // Minimalna warto�� kalorii zale�na od p�ci
            int minCalories = user.Plec == Gender.Mezczyzna ? 1500 : 1200;

            // Upewnij si�, �e deficyt nie jest zbyt niski
            if (deficit < minCalories)
                deficit = minCalories;

            return deficit;
        }

        private void CalculateMacronutrients(User user)
        {
            // Je�li nie mamy danych o kaloriach, nie mo�emy obliczy� makrosk�adnik�w
            if (user.CustomCaloriesDeficit <= 0 || !user.Waga.HasValue)
            {
                user.CustomProteinGrams = 0;
                user.CustomCarbsGrams = 0;
                user.CustomFatGrams = 0;
                return;
            }

            // Obliczanie makrosk�adnik�w w zale�no�ci od celu i p�ci
            if (user.Cel == UserGoal.Schudniecie)
            {
                // Wy�sze bia�ko, ni�sze w�glowodany, umiarkowane t�uszcze dla redukcji
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 2.0 : 1.8;
                user.CustomProteinGrams = (int)(user.Waga.Value * proteinMultiplier); // Wi�cej bia�ka na kg masy cia�a
                user.CustomFatGrams = (int)(user.CustomCaloriesDeficit * 0.25 / 9); // 25% kalorii z t�uszcz�w
                user.CustomCarbsGrams = (int)((user.CustomCaloriesDeficit - (user.CustomProteinGrams.Value * 4) - (user.CustomFatGrams.Value * 9)) / 4);
            }
            else if (user.Cel == UserGoal.PrzybranieMasy)
            {
                // Wysokie bia�ko, wysokie w�glowodany, umiarkowane t�uszcze dla zwi�kszenia masy
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 1.8 : 1.6;
                user.CustomProteinGrams = (int)(user.Waga.Value * proteinMultiplier);
                user.CustomFatGrams = (int)(user.CustomCaloriesDeficit * 0.25 / 9); // 25% kalorii z t�uszcz�w
                user.CustomCarbsGrams = (int)((user.CustomCaloriesDeficit - (user.CustomProteinGrams.Value * 4) - (user.CustomFatGrams.Value * 9)) / 4);
            }
            else // UserGoal.ZdroweNawyki
            {
                // Zr�wnowa�ony stosunek makrosk�adnik�w dla zdrowych nawyk�w
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 1.6 : 1.4;
                user.CustomProteinGrams = (int)(user.Waga.Value * proteinMultiplier);
                user.CustomFatGrams = (int)(user.CustomCaloriesDeficit * 0.3 / 9); // 30% kalorii z t�uszcz�w
                user.CustomCarbsGrams = (int)((user.CustomCaloriesDeficit - (user.CustomProteinGrams.Value * 4) - (user.CustomFatGrams.Value * 9)) / 4);
            }

            // Uwzgl�dnienie aktywno�ci fizycznej przy obliczaniu w�glowodan�w
            if (user.AktywnoscFizyczna != null && user.AktywnoscFizyczna.Contains("4-5"))
            {
                // Wi�cej w�glowodan�w dla os�b aktywnych fizycznie
                int extraCarbs = (int)(user.Waga.Value * 0.5); // Dodatkowe 0.5g w�glowodan�w na kg masy cia�a
                user.CustomCarbsGrams += extraCarbs;
            }

            // Upewnij si�, �e warto�ci nie s� ujemne lub zbyt niskie
            if (user.CustomCarbsGrams < 50) user.CustomCarbsGrams = 50; // Minimalna warto�� w�glowodan�w
            if (user.CustomProteinGrams < 40) user.CustomProteinGrams = 40; // Minimalna warto�� bia�ka
            if (user.CustomFatGrams < 20) user.CustomFatGrams = 20; // Minimalna warto�� t�uszcz�w
        }

        public class UserEditViewModel
        {
            [Required(ErrorMessage = "Nazwa u�ytkownika jest wymagana")]
            [StringLength(50, ErrorMessage = "Nazwa u�ytkownika musi mie� od {2} do {1} znak�w.", MinimumLength = 3)]
            public string Username { get; set; }

            [Required(ErrorMessage = "Email jest wymagany")]
            [EmailAddress(ErrorMessage = "Nieprawid�owy format adresu email")]
            public string Email { get; set; }

            [Display(Name = "Wiek")]
            [Range(16, 100, ErrorMessage = "Wiek musi by� z zakresu od {1} do {2} lat.")]
            public int? Wiek { get; set; }

            [Display(Name = "P�e�")]
            public Gender? Plec { get; set; }

            [Display(Name = "Waga (kg)")]
            [Range(30, 250, ErrorMessage = "Waga musi by� z zakresu od {1} do {2} kg.")]
            public double? Waga { get; set; }

            [Display(Name = "Wzrost (cm)")]
            [Range(120, 250, ErrorMessage = "Wzrost musi by� z zakresu od {1} do {2} cm.")]
            public double? Wzrost { get; set; }

            [Display(Name = "Aktywno�� fizyczna")]
            public string AktywnoscFizyczna { get; set; }

            [Display(Name = "Rodzaj pracy")]
            public string RodzajPracy { get; set; }

            [Display(Name = "Cel")]
            public UserGoal? Cel { get; set; }
        }
    }
}