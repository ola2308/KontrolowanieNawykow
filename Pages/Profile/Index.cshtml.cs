using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text;

namespace KontrolaNawykow.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------------------
        // W³asna klasa DTO (ViewModel) 
        // -------------------------------
        // Nie zapisywana w bazie – s³u¿y tylko do wyœwietlenia listy zakupów
        public class ShoppingListItem
        {
            public int IngredientId { get; set; }
            public string IngredientName { get; set; }
            public float TotalAmount { get; set; }
        }

        // ------------------------------------------------------------
        // W³aœciwoœci (pola) widoku
        // ------------------------------------------------------------
        public User CurrentUser { get; set; }
        public double BMI { get; set; }
        public string BMICategory { get; set; }
        public int TotalCalories { get; set; }
        public int TotalRecipes { get; set; }
        public int TotalMealPlans { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }

        // Zmieniamy typ z List<Ingredient> na List<ShoppingListItem>
        public List<ShoppingListItem> ShoppingList { get; set; } = new List<ShoppingListItem>();

        // ------------------------------------------------------------
        // OnGetAsync – pobieranie danych u¿ytkownika, obliczenia BMI itd.
        // ------------------------------------------------------------
        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Pobierz dane u¿ytkownika
            CurrentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (CurrentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Oblicz BMI (jeœli waga i wzrost s¹ wype³nione)
            if (CurrentUser.Wzrost.HasValue && CurrentUser.Waga.HasValue)
            {
                double heightInMeters = CurrentUser.Wzrost.Value / 100.0;
                BMI = CurrentUser.Waga.Value / (heightInMeters * heightInMeters);
                BMICategory = GetBMICategory(BMI);
            }

            // Statystyki
            TotalRecipes = await _context.Recipes.CountAsync(r => r.UserId == userId);
            TotalMealPlans = await _context.MealPlans.CountAsync(mp => mp.UserId == userId);
            CompletedTasks = await _context.ToDos.CountAsync(t => t.UserId == userId && t.IsCompleted);
            PendingTasks = await _context.ToDos.CountAsync(t => t.UserId == userId && !t.IsCompleted);

            // Liczba kalorii z zaplanowanych posi³ków na dziœ
            var today = DateTime.Today;
            var meals = await _context.MealPlans
                .Where(mp => mp.UserId == userId && mp.Date.HasValue && mp.Date.Value.Date == today)
                .Include(mp => mp.Recipe)
                .ToListAsync();

            foreach (var meal in meals)
            {
                if (meal.Recipe != null)
                {
                    TotalCalories += meal.Recipe.Calories;
                }
            }

            // Pobierz listê zakupów – na najbli¿szy tydzieñ
            ShoppingList = await GetShoppingListAsync(userId);

            return Page();
        }

        // ------------------------------------------------------------
        // Obliczanie kategorii BMI
        // ------------------------------------------------------------
        private string GetBMICategory(double bmi)
        {
            if (bmi < 16)
                return "Wyg³odzenie";
            else if (bmi < 17)
                return "Wychudzenie";
            else if (bmi < 18.5)
                return "Niedowaga";
            else if (bmi < 25)
                return "Waga prawid³owa";
            else if (bmi < 30)
                return "Nadwaga";
            else if (bmi < 35)
                return "Oty³oœæ I stopnia";
            else if (bmi < 40)
                return "Oty³oœæ II stopnia";
            else
                return "Oty³oœæ III stopnia";
        }

        // ------------------------------------------------------------
        // Generowanie listy zakupów z najbli¿szego tygodnia
        // bez u¿ywania Ingredient.Amount w bazie
        // ------------------------------------------------------------
        private async Task<List<ShoppingListItem>> GetShoppingListAsync(int userId)
        {
            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(7);

            var mealPlans = await _context.MealPlans
                .Where(mp => mp.UserId == userId
                             && mp.Date.HasValue
                             && mp.Date.Value >= startDate
                             && mp.Date.Value <= endDate
                             && mp.RecipeId.HasValue)
                .Include(mp => mp.Recipe)
                    .ThenInclude(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                .ToListAsync();

            // S³ownik: klucz = IngredientId, wartoœæ = ShoppingListItem (sumujemy Amount)
            var shoppingDict = new Dictionary<int, ShoppingListItem>();

            foreach (var meal in mealPlans)
            {
                if (meal.Recipe?.RecipeIngredients == null)
                    continue;

                foreach (var recipeIngredient in meal.Recipe.RecipeIngredients)
                {
                    if (recipeIngredient.Ingredient != null)
                    {
                        float amount = recipeIngredient.Amount ?? 0f;

                        if (shoppingDict.TryGetValue(recipeIngredient.IngredientId, out var existing))
                        {
                            // Dodajemy do ju¿ istniej¹cej sumy
                            existing.TotalAmount += amount;
                        }
                        else
                        {
                            // Tworzymy nowy obiekt ShoppingListItem
                            shoppingDict[recipeIngredient.IngredientId] = new ShoppingListItem
                            {
                                IngredientId = recipeIngredient.IngredientId,
                                IngredientName = recipeIngredient.Ingredient.Name,
                                TotalAmount = amount
                            };
                        }
                    }
                }
            }

            return shoppingDict.Values.ToList();
        }

        // ------------------------------------------------------------
        // Wygenerowanie pliku (np. .txt) z list¹ zakupów
        // ------------------------------------------------------------
        public async Task<IActionResult> OnPostGenerateShoppingListAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var shoppingItems = await GetShoppingListAsync(userId);

            // Prosty przyk³ad – tworzymy tekst do pobrania
            var shoppingListText = "Lista zakupów na nadchodz¹cy tydzieñ:\n\n";
            foreach (var item in shoppingItems)
            {
                shoppingListText += $"- {item.IngredientName}: {item.TotalAmount} g\n";
            }

            // Konwersja do bajtów
            byte[] fileBytes = Encoding.UTF8.GetBytes(shoppingListText);

            // Zwróæ plik do pobrania jako plain text
            return File(fileBytes, "text/plain", "lista_zakupow.txt");
        }
    }
}
