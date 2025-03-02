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

            // Inicjalizacja modelu z danymi u¿ytkownika
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

            // Sprawdzenie unikalnoœci nazwy u¿ytkownika i emaila
            if (user.Username != UserEdit.Username)
            {
                var existingUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == UserEdit.Username);
                if (existingUsername != null)
                {
                    ModelState.AddModelError("UserEdit.Username", "Nazwa u¿ytkownika jest ju¿ zajêta.");
                    return Page();
                }
            }

            if (user.Email != UserEdit.Email)
            {
                var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == UserEdit.Email);
                if (existingEmail != null)
                {
                    ModelState.AddModelError("UserEdit.Email", "Adres email jest ju¿ u¿ywany.");
                    return Page();
                }
            }

            // Aktualizacja danych u¿ytkownika
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

                // Zaktualizuj równie¿ kalorie i makrosk³adniki
                int caloriesDeficit = CalculateCaloriesDeficit(user);
                user.CustomCaloriesDeficit = caloriesDeficit;

                // Oblicz i zapisz makrosk³adniki
                CalculateMacronutrients(user);
            }

            // Zapisz zmiany
            try
            {
                await _context.SaveChangesAsync();
                SuccessMessage = "Dane profilu zosta³y zaktualizowane.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Wyst¹pi³ b³¹d podczas zapisywania zmian: {ex.Message}";
                return Page();
            }

            return RedirectToPage("./Index");
        }

        private int CalculateCaloriesDeficit(User user)
        {
            // Sprawdzenie czy mamy wszystkie niezbêdne dane
            if (!user.Waga.HasValue || !user.Wzrost.HasValue || !user.Wiek.HasValue)
            {
                return 2000; // Wartoœæ domyœlna jeœli brakuje danych
            }

            // Podstawowe zapotrzebowanie kaloryczne (BMR) - wzór Mifflina-St Jeora
            double bmr;

            // BMR w zale¿noœci od p³ci
            if (user.Plec == Gender.Mezczyzna)
            {
                // Dla mê¿czyzn: BMR = 10 * waga + 6.25 * wzrost - 5 * wiek + 5
                bmr = 10 * user.Waga.Value + 6.25 * user.Wzrost.Value - 5 * user.Wiek.Value + 5;
            }
            else
            {
                // Dla kobiet: BMR = 10 * waga + 6.25 * wzrost - 5 * wiek - 161
                bmr = 10 * user.Waga.Value + 6.25 * user.Wzrost.Value - 5 * user.Wiek.Value - 161;
            }

            // Wspó³czynnik aktywnoœci fizycznej (PAL)
            double pal = 1.2; // Domyœlnie siedz¹cy tryb ¿ycia

            // Uwzglêdnienie aktywnoœci fizycznej
            if (user.AktywnoscFizyczna != null)
            {
                if (user.AktywnoscFizyczna.Contains("0 treningów"))
                    pal = 1.2; // Siedz¹cy tryb ¿ycia
                else if (user.AktywnoscFizyczna.Contains("1-3"))
                    pal = 1.375; // Lekka aktywnoœæ
                else if (user.AktywnoscFizyczna.Contains("4-5"))
                    pal = 1.55; // Umiarkowana aktywnoœæ
            }

            // Uwzglêdnienie rodzaju pracy
            if (user.RodzajPracy != null)
            {
                if (user.RodzajPracy == "Fizyczna")
                    pal += 0.1; // Dodatkowa aktywnoœæ dla pracy fizycznej
                else if (user.RodzajPracy == "Pó³ na pó³")
                    pal += 0.05; // Dodatkowa aktywnoœæ dla pracy mieszanej
            }

            // Ca³kowite dzienne zapotrzebowanie energetyczne (TDEE)
            double tdee = bmr * pal;

            // Uwzglêdnienie wieku (metabolizm spada z wiekiem)
            if (user.Wiek > 40)
                tdee *= 0.98; // Lekka redukcja dla osób powy¿ej 40 lat
            if (user.Wiek > 60)
                tdee *= 0.97; // Dalsza redukcja dla osób powy¿ej 60 lat

            // Deficyt kaloryczny zale¿ny od celu
            int deficit = 0;

            if (user.Cel == UserGoal.Schudniecie)
            {
                // Deficyt dla schudniêcia (oko³o 20% TDEE)
                deficit = (int)(tdee * 0.8);
            }
            else if (user.Cel == UserGoal.PrzybranieMasy)
            {
                // Nadwy¿ka dla przybrania masy (oko³o 10% TDEE)
                deficit = (int)(tdee * 1.1);
            }
            else // UserGoal.ZdroweNawyki
            {
                // Utrzymanie wagi
                deficit = (int)tdee;
            }

            // Minimalna wartoœæ kalorii zale¿na od p³ci
            int minCalories = user.Plec == Gender.Mezczyzna ? 1500 : 1200;

            // Upewnij siê, ¿e deficyt nie jest zbyt niski
            if (deficit < minCalories)
                deficit = minCalories;

            return deficit;
        }

        private void CalculateMacronutrients(User user)
        {
            // Jeœli nie mamy danych o kaloriach, nie mo¿emy obliczyæ makrosk³adników
            if (user.CustomCaloriesDeficit <= 0 || !user.Waga.HasValue)
            {
                user.CustomProteinGrams = 0;
                user.CustomCarbsGrams = 0;
                user.CustomFatGrams = 0;
                return;
            }

            // Obliczanie makrosk³adników w zale¿noœci od celu i p³ci
            if (user.Cel == UserGoal.Schudniecie)
            {
                // Wy¿sze bia³ko, ni¿sze wêglowodany, umiarkowane t³uszcze dla redukcji
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 2.0 : 1.8;
                user.CustomProteinGrams = (int)(user.Waga.Value * proteinMultiplier); // Wiêcej bia³ka na kg masy cia³a
                user.CustomFatGrams = (int)(user.CustomCaloriesDeficit * 0.25 / 9); // 25% kalorii z t³uszczów
                user.CustomCarbsGrams = (int)((user.CustomCaloriesDeficit - (user.CustomProteinGrams.Value * 4) - (user.CustomFatGrams.Value * 9)) / 4);
            }
            else if (user.Cel == UserGoal.PrzybranieMasy)
            {
                // Wysokie bia³ko, wysokie wêglowodany, umiarkowane t³uszcze dla zwiêkszenia masy
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 1.8 : 1.6;
                user.CustomProteinGrams = (int)(user.Waga.Value * proteinMultiplier);
                user.CustomFatGrams = (int)(user.CustomCaloriesDeficit * 0.25 / 9); // 25% kalorii z t³uszczów
                user.CustomCarbsGrams = (int)((user.CustomCaloriesDeficit - (user.CustomProteinGrams.Value * 4) - (user.CustomFatGrams.Value * 9)) / 4);
            }
            else // UserGoal.ZdroweNawyki
            {
                // Zrównowa¿ony stosunek makrosk³adników dla zdrowych nawyków
                double proteinMultiplier = user.Plec == Gender.Mezczyzna ? 1.6 : 1.4;
                user.CustomProteinGrams = (int)(user.Waga.Value * proteinMultiplier);
                user.CustomFatGrams = (int)(user.CustomCaloriesDeficit * 0.3 / 9); // 30% kalorii z t³uszczów
                user.CustomCarbsGrams = (int)((user.CustomCaloriesDeficit - (user.CustomProteinGrams.Value * 4) - (user.CustomFatGrams.Value * 9)) / 4);
            }

            // Uwzglêdnienie aktywnoœci fizycznej przy obliczaniu wêglowodanów
            if (user.AktywnoscFizyczna != null && user.AktywnoscFizyczna.Contains("4-5"))
            {
                // Wiêcej wêglowodanów dla osób aktywnych fizycznie
                int extraCarbs = (int)(user.Waga.Value * 0.5); // Dodatkowe 0.5g wêglowodanów na kg masy cia³a
                user.CustomCarbsGrams += extraCarbs;
            }

            // Upewnij siê, ¿e wartoœci nie s¹ ujemne lub zbyt niskie
            if (user.CustomCarbsGrams < 50) user.CustomCarbsGrams = 50; // Minimalna wartoœæ wêglowodanów
            if (user.CustomProteinGrams < 40) user.CustomProteinGrams = 40; // Minimalna wartoœæ bia³ka
            if (user.CustomFatGrams < 20) user.CustomFatGrams = 20; // Minimalna wartoœæ t³uszczów
        }

        public class UserEditViewModel
        {
            [Required(ErrorMessage = "Nazwa u¿ytkownika jest wymagana")]
            [StringLength(50, ErrorMessage = "Nazwa u¿ytkownika musi mieæ od {2} do {1} znaków.", MinimumLength = 3)]
            public string Username { get; set; }

            [Required(ErrorMessage = "Email jest wymagany")]
            [EmailAddress(ErrorMessage = "Nieprawid³owy format adresu email")]
            public string Email { get; set; }

            [Display(Name = "Wiek")]
            [Range(16, 100, ErrorMessage = "Wiek musi byæ z zakresu od {1} do {2} lat.")]
            public int? Wiek { get; set; }

            [Display(Name = "P³eæ")]
            public Gender? Plec { get; set; }

            [Display(Name = "Waga (kg)")]
            [Range(30, 250, ErrorMessage = "Waga musi byæ z zakresu od {1} do {2} kg.")]
            public double? Waga { get; set; }

            [Display(Name = "Wzrost (cm)")]
            [Range(120, 250, ErrorMessage = "Wzrost musi byæ z zakresu od {1} do {2} cm.")]
            public double? Wzrost { get; set; }

            [Display(Name = "Aktywnoœæ fizyczna")]
            public string AktywnoscFizyczna { get; set; }

            [Display(Name = "Rodzaj pracy")]
            public string RodzajPracy { get; set; }

            [Display(Name = "Cel")]
            public UserGoal? Cel { get; set; }
        }
    }
}