using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KontrolaNawykow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        public Dictionary<DateTime, List<MealPlan>> MealPlans { get; set; } = new();
        public List<Recipe> Recipes { get; set; } = new();
        public List<Ingredient> Ingredients { get; set; } = new();
        public User CurrentUser { get; set; }

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
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/Account/Login");
                }

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return RedirectToPage("/Account/Login");
                }

                CurrentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (CurrentUser == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                WeekDays = GetWeekDays(WeekOffset);
                Recipes = await _context.Recipes
                    .Where(r => r.UserId == userId || r.IsPublic)
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .ToListAsync();

                Ingredients = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync();

                var startDate = WeekDays.First().Date;
                var endDate = WeekDays.Last().Date.AddDays(1).AddSeconds(-1);

                var mealPlans = await _context.MealPlans
                    .Where(mp => mp.UserId == userId && mp.Date >= startDate && mp.Date <= endDate)
                    .Include(mp => mp.Recipe)
                        .ThenInclude(r => r.RecipeIngredients)
                            .ThenInclude(ri => ri.Ingredient)
                    .OrderBy(mp => mp.Date)
                    .ThenBy(mp => mp.MealType)
                    .ToListAsync();

                if (mealPlans.Any())
                {
                    MealPlans = mealPlans.GroupBy(mp => mp.Date.Value.Date).ToDictionary(g => g.Key, g => g.ToList());
                }

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"B³¹d podczas ³adowania strony diety: {ex.Message}");
                return RedirectToPage("/Error");
            }
        }

        private List<DayInfo> GetWeekDays(int weekOffset = 0)
        {
            var days = new List<DayInfo>();
            var today = DateTime.Today;
            var monday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday)
                monday = monday.AddDays(-7);

            monday = monday.AddDays(weekOffset * 7);

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

        private string GetPolishDayName(DayOfWeek dayOfWeek) => dayOfWeek switch
        {
            DayOfWeek.Monday => "Poniedzia³ek",
            DayOfWeek.Tuesday => "Wtorek",
            DayOfWeek.Wednesday => "Œroda",
            DayOfWeek.Thursday => "Czwartek",
            DayOfWeek.Friday => "Pi¹tek",
            DayOfWeek.Saturday => "Sobota",
            DayOfWeek.Sunday => "Niedziela",
            _ => string.Empty,
        };

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
                }
            }

            return totals;
        }
    }
}
