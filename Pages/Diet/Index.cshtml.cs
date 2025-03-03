using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KontrolaNawykow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KontrolaNawykow.Pages.Diet
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<DayInfo> WeekDays { get; set; }
        public Dictionary<DateTime, List<MealPlan>> MealPlans { get; set; } = new Dictionary<DateTime, List<MealPlan>>();
        public List<Recipe> Recipes { get; set; } = new List<Recipe>();
        public List<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
        public User CurrentUser { get; set; }

        // Parametr dla nawigacji tygodniowej
        [BindProperty(SupportsGet = true)]
        public int WeekOffset { get; set; } = 0;

        public class DayInfo
        {
            public string Name { get; set; }
            public DateTime Date { get; set; }
            public bool IsToday { get; set; }
        }

        public class DailyTotals
        {
            public int Calories { get; set; } = 0;
            public float Protein { get; set; } = 0;
            public float Fat { get; set; } = 0;
            public float Carbs { get; set; } = 0;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Pobierz ID zalogowanego u�ytkownika
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Account/Login");
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToPage("/Account/Login");
                }

                // Pobierz dane u�ytkownika
                CurrentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (CurrentUser == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                // Przygotuj informacje o dniach tygodnia z uwzgl�dnieniem offsetu
                WeekDays = GetWeekDays(WeekOffset);

                // Pobierz przepisy u�ytkownika i publiczne
                Recipes = await _context.Recipes
                    .Where(r => r.UserId == userId || r.IsPublic)
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .ToListAsync();

                // Pobierz sk�adniki
                Ingredients = await _context.Ingredients
                    .OrderBy(i => i.Name)
                    .ToListAsync();

                // Przygotuj daty dla zapyta�
                var startDate = WeekDays.First().Date;
                var endDate = WeekDays.Last().Date.AddDays(1).AddSeconds(-1); // Koniec ostatniego dnia

                // Pobierz plany posi�k�w
                var mealPlans = await _context.MealPlans
                    .Where(mp => mp.UserId == userId &&
                           mp.Date >= startDate && mp.Date <= endDate)
                    .Include(mp => mp.Recipe)
                        .ThenInclude(r => r.RecipeIngredients)
                            .ThenInclude(ri => ri.Ingredient)
                    .OrderBy(mp => mp.Date)
                    .ThenBy(mp => mp.MealType)
                    .ToListAsync();

                // Grupuj posi�ki wed�ug daty
                foreach (var mealPlan in mealPlans)
                {
                    if (mealPlan.Date.HasValue)
                    {
                        var date = mealPlan.Date.Value.Date;
                        if (!MealPlans.ContainsKey(date))
                        {
                            MealPlans[date] = new List<MealPlan>();
                        }
                        MealPlans[date].Add(mealPlan);
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                // Logowanie b��du
                Console.WriteLine($"B��d podczas �adowania strony diety: {ex.Message}");
                return RedirectToPage("/Error", new { message = ex.Message });
            }
        }

        // Metoda zwracaj�ca informacje o dniach tygodnia (poniedzia�ek-niedziela) z uwzgl�dnieniem offsetu
        private List<DayInfo> GetWeekDays(int weekOffset = 0)
        {
            var days = new List<DayInfo>();
            var today = DateTime.Today;

            // Znajd� poniedzia�ek bie��cego tygodnia
            var monday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday)
                monday = monday.AddDays(-7); // Je�li dzi� niedziela, cofnij do poprzedniego poniedzia�ku

            // Zastosuj offset tygodniowy
            monday = monday.AddDays(weekOffset * 7);

            // Dodaj 7 dni od poniedzia�ku
            for (int i = 0; i < 7; i++)
            {
                var date = monday.AddDays(i);
                days.Add(new DayInfo
                {
                    Name = GetPolishDayName(date.DayOfWeek),
                    Date = date,
                    IsToday = date.Date == today
                });
            }

            return days;
        }

        // Metoda zwracaj�ca polskie nazwy dni tygodnia
        private string GetPolishDayName(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return "Poniedzia�ek";
                case DayOfWeek.Tuesday: return "Wtorek";
                case DayOfWeek.Wednesday: return "�roda";
                case DayOfWeek.Thursday: return "Czwartek";
                case DayOfWeek.Friday: return "Pi�tek";
                case DayOfWeek.Saturday: return "Sobota";
                case DayOfWeek.Sunday: return "Niedziela";
                default: return string.Empty;
            }
        }

        // Metoda obliczaj�ca ca�kowit� warto�� od�ywcz� posi�k�w dla danego dnia
        public DailyTotals GetDailyTotals(DateTime date)
        {
            var totals = new DailyTotals();

            if (MealPlans.ContainsKey(date))
            {
                foreach (var meal in MealPlans[date])
                {
                    if (meal.Recipe != null)
                    {
                        totals.Calories += meal.Recipe.Calories;
                        totals.Protein += meal.Recipe.Protein;
                        totals.Fat += meal.Recipe.Fat;
                        totals.Carbs += meal.Recipe.Carbs;
                    }
                    else
                    {
                        // Pr�ba analizy zawarto�ci CustomEntry, je�li istnieje
                        // Np. je�li zawiera informacje o makrosk�adnikach
                        // To logika, kt�r� mo�esz zaimplementowa� w przysz�o�ci
                    }
                }
            }

            // Zaokr�glanie warto�ci dla lepszej czytelno�ci
            totals.Protein = (float)Math.Round(totals.Protein, 1);
            totals.Fat = (float)Math.Round(totals.Fat, 1);
            totals.Carbs = (float)Math.Round(totals.Carbs, 1);

            return totals;
        }

        // Metoda pomocnicza do debugowania - zwraca dost�pne sk�adniki dla przepisu
        public List<RecipeIngredient> GetIngredients(int recipeId)
        {
            var recipe = Recipes.FirstOrDefault(r => r.Id == recipeId);
            return recipe?.RecipeIngredients?.ToList() ?? new List<RecipeIngredient>();
        }

        // Pomocnicza metoda do pobierania zjedzone/niezjedzone posi�ki na dany dzie�
        public List<MealPlan> GetMealsByStatus(DateTime date, bool eaten)
        {
            if (MealPlans.ContainsKey(date))
            {
                return MealPlans[date].Where(m => m.Eaten == eaten).ToList();
            }
            return new List<MealPlan>();
        }

        // Metoda zwracaj�ca kalorie dla aktualnego u�ytkownika (mo�na doda� kalkulacje na podstawie wagi, wzrostu, aktywno�ci)
        public int GetRecommendedCalories()
        {
            if (CurrentUser == null)
                return 2000; // Domy�lna warto��

            // Tutaj mo�esz doda� bardziej zaawansowan� logik� na podstawie profilu u�ytkownika
            // Np. wykorzystuj�c Harris-Benedict lub inne r�wnania dla BMR i TDEE

            if (CurrentUser.Plec == Gender.Mezczyzna)
            {
                return 2500; // Przyk�adowa warto�� dla m�czyzny
            }
            else if (CurrentUser.Plec == Gender.Kobieta)
            {
                return 2000; // Przyk�adowa warto�� dla kobiety
            }

            return 2200; // Domy�lna warto��, je�li p�e� nie jest okre�lona
        }

        // Metoda do sprawdzania czy u�ytkownik uko�czy� swoje cele �ywieniowe na dany dzie�
        public bool IsNutritionGoalCompleted(DateTime date)
        {
            var totals = GetDailyTotals(date);
            var recommendedCalories = GetRecommendedCalories();

            // Przyk�adowa logika - uznajemy cel za uko�czony, je�li spo�ycie kalorii 
            // mie�ci si� w zakresie 90-110% zalecanego poziomu
            var minCalories = recommendedCalories * 0.9;
            var maxCalories = recommendedCalories * 1.1;

            return totals.Calories >= minCalories && totals.Calories <= maxCalories;
        }
    }
}