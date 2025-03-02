using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KontrolaNawykow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace KontrolaNawykow.Pages.ToDo
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
        public Dictionary<DateTime, List<Models.ToDo>> ToDos { get; set; } = new Dictionary<DateTime, List<Models.ToDo>>();
        public User CurrentUser { get; set; }
        public List<Models.ToDo> SavedActivities { get; set; } = new List<Models.ToDo>();

        // Parametr przekazywany w adresie URL do nawigacji tygodniowej
        [BindProperty(SupportsGet = true)]
        public int WeekOffset { get; set; } = 0;

        public class DayInfo
        {
            public string Name { get; set; }
            public DateTime Date { get; set; }
            public bool IsToday { get; set; }
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

                // Przygotuj daty dla zapyta�
                var startDate = WeekDays.First().Date;
                var endDate = WeekDays.Last().Date.AddDays(1).AddSeconds(-1); // Koniec ostatniego dnia

                // Pobierz zadania ToDo
                var todos = await _context.ToDos
                    .Where(t => t.UserId == userId &&
                           !t.IsTemplate && // Wykluczamy szablony
                           t.CreatedAt.Date >= startDate && t.CreatedAt.Date <= endDate)
                    .OrderBy(t => t.CreatedAt)
                    .ToListAsync();

                // Grupuj zadania wed�ug daty
                if (todos.Any())
                {
                    ToDos = todos
                        .GroupBy(t => t.CreatedAt.Date)
                        .ToDictionary(g => g.Key, g => g.ToList());
                }

                // Pobierz zapisane aktywno�ci (szablony)
                SavedActivities = await _context.ToDos
                    .Where(t => t.UserId == userId && t.IsTemplate == true)
                    .OrderBy(t => t.Task)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                // Logowanie b��du
                Console.WriteLine($"B��d podczas �adowania strony ToDo: {ex.Message}");
                return RedirectToPage("/Error");
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
    }
}