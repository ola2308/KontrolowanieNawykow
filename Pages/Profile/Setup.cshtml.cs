using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using KontrolaNawykow.Models;

namespace KontrolaNawykow.Pages.Profile
{
    [Authorize]
    public class SetupModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SetupModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int? Wiek { get; set; }

        [BindProperty]
        public double? Waga { get; set; }

        [BindProperty]
        public double? Wzrost { get; set; }

        [BindProperty]
        public string AktywnoscFizyczna { get; set; }

        [BindProperty]
        public string RodzajPracy { get; set; }

        [BindProperty]
        public UserGoal Cel { get; set; }

        public string UserName { get; set; }
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Ustawiamy nazwê u¿ytkownika 
            UserName = user.Username;

            // Jeœli u¿ytkownik ju¿ ma wype³nione dane, wczytujemy je
            if (user.Wiek.HasValue || user.Waga.HasValue || user.Wzrost.HasValue)
            {
                Wiek = user.Wiek;
                Waga = user.Waga;
                Wzrost = user.Wzrost;
                AktywnoscFizyczna = user.AktywnoscFizyczna;
                RodzajPracy = user.RodzajPracy;
                Cel = (UserGoal)user.Cel;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            UserName = user.Username;

            if (!Wiek.HasValue)
            {
                ErrorMessage = "Proszê podaæ wiek.";
                return Page();
            }

            if (!Waga.HasValue)
            {
                ErrorMessage = "Proszê podaæ wagê.";
                return Page();
            }

            if (!Wzrost.HasValue)
            {
                ErrorMessage = "Proszê podaæ wzrost.";
                return Page();
            }

            if (string.IsNullOrEmpty(AktywnoscFizyczna))
            {
                ErrorMessage = "Proszê wybraæ poziom aktywnoœci fizycznej.";
                return Page();
            }

            if (string.IsNullOrEmpty(RodzajPracy))
            {
                ErrorMessage = "Proszê wybraæ rodzaj pracy.";
                return Page();
            }

            // Aktualizacja danych u¿ytkownika
            user.Wiek = Wiek;
            user.Waga = Waga;
            user.Wzrost = Wzrost;
            user.AktywnoscFizyczna = AktywnoscFizyczna;
            user.RodzajPracy = RodzajPracy;
            user.Cel = Cel;

            try
            {
                await _context.SaveChangesAsync();
                SuccessMessage = "Twoje dane zosta³y zapisane pomyœlnie!";

                // Przekierowanie do strony g³ównej po zapisaniu
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Wyst¹pi³ b³¹d podczas zapisywania danych. Spróbuj ponownie.";
                // Mo¿na dodaæ logowanie b³êdu
                return Page();
            }
        }
    }
}