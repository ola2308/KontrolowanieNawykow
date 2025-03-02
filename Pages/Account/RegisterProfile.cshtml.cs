using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace KontrolaNawykow.Pages.Account
{
    public class RegisterProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterProfileModel(ApplicationDbContext context)
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
        public Gender Plec { get; set; }
        [BindProperty]
        public UserGoal Cel { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Sprawdzenie czy ID u�ytkownika jest w TempData
            if (TempData["UserId"] == null)
            {
                Console.WriteLine("? Brak UserId w TempData. Przekierowanie do rejestracji.");
                return RedirectToPage("/Account/Register");
            }

            int userId = (int)TempData["UserId"];
            Console.WriteLine($"? Pobrano UserId z TempData: {userId}");

            // Sprawdzenie czy u�ytkownik istnieje
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"? Nie znaleziono u�ytkownika o ID: {userId}");
                return RedirectToPage("/Account/Register");
            }

            // Zachowanie ID w TempData
            TempData.Keep("UserId");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Console.WriteLine("?? Pr�ba zapisania profilu u�ytkownika");

                // Debugowanie formularza
                Console.WriteLine("?? DANE FORMULARZA:");
                foreach (var key in Request.Form.Keys)
                {
                    Console.WriteLine($"   Klucz: {key}, Warto��: {Request.Form[key]}");
                }

                // Pobranie ID u�ytkownika z TempData
                if (TempData["UserId"] == null)
                {
                    ErrorMessage = "Wyst�pi� b��d: brak ID u�ytkownika.";
                    Console.WriteLine("? Brak UserId w TempData.");
                    return RedirectToPage("/Account/Register");
                }

                int userId = (int)TempData["UserId"];
                Console.WriteLine($"? Pobrano UserId z TempData: {userId}");

                // Znalezienie u�ytkownika w bazie danych
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    ErrorMessage = "Nie znaleziono u�ytkownika.";
                    Console.WriteLine($"? Nie znaleziono u�ytkownika o ID: {userId}");
                    return RedirectToPage("/Account/Register");
                }

                // Walidacja ModelState
                if (!ModelState.IsValid)
                {
                    TempData.Keep("UserId");
                    return Page();
                }

                // Walidacja p�l
                if (!Wiek.HasValue)
                {
                    ErrorMessage = "Prosz� poda� wiek.";
                    TempData.Keep("UserId");
                    return Page();
                }

                if (!Waga.HasValue)
                {
                    ErrorMessage = "Prosz� poda� wag�.";
                    TempData.Keep("UserId");
                    return Page();
                }

                if (!Wzrost.HasValue)
                {
                    ErrorMessage = "Prosz� poda� wzrost.";
                    TempData.Keep("UserId");
                    return Page();
                }

                if (string.IsNullOrEmpty(AktywnoscFizyczna))
                {
                    ErrorMessage = "Prosz� wybra� poziom aktywno�ci fizycznej.";
                    TempData.Keep("UserId");
                    return Page();
                }

                if (string.IsNullOrEmpty(RodzajPracy))
                {
                    ErrorMessage = "Prosz� wybra� rodzaj pracy.";
                    TempData.Keep("UserId");
                    return Page();
                }

                // Aktualizacja danych u�ytkownika
                user.Plec = Plec;
                user.Wiek = Wiek;
                user.Waga = Waga;
                user.Wzrost = Wzrost;
                user.AktywnoscFizyczna = AktywnoscFizyczna;
                user.RodzajPracy = RodzajPracy;
                user.Cel = Cel;

                // Zapisanie zmian w bazie danych
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                Console.WriteLine($"? Zaktualizowano dane u�ytkownika o ID: {user.Id}");

                // Automatyczne logowanie u�ytkownika po rejestracji
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

                // Przekierowanie do strony g��wnej
                return RedirectToPage("/Account/RegisterDietModel");
            }
            catch (Exception ex)
            {
                // Logowanie b��du
                ErrorMessage = "Wyst�pi� b��d podczas rejestracji: " + ex.Message;
                Console.WriteLine("? B��d: " + ex.Message);
                Console.WriteLine("? Stack trace: " + ex.StackTrace);

                // Zachowaj ID u�ytkownika
                TempData.Keep("UserId");
                return Page();
            }
        }
    }
}