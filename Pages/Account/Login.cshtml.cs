using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace KontrolaNawyków.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Nazwa u¿ytkownika jest wymagana")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Has³o jest wymagane")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public void OnGet()
        {
            // Jeœli zostaliœmy przekierowani z pomyœlnej rejestracji
            if (TempData["RegisterSuccess"] != null)
            {
                SuccessMessage = TempData["RegisterSuccess"].ToString();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Console.WriteLine("?? Próba logowania - pocz¹tek metody OnPostAsync");

                // Rêcznie pobierz wartoœci z formularza
                string username = Request.Form["Username"].ToString();
                string password = Request.Form["Password"].ToString();

                Console.WriteLine($"?? Rêcznie pobrane wartoœci: Username='{username}', Password length={password.Length}");

                // Przypisz wartoœci do w³aœciwoœci modelu
                Username = username;
                Password = password;

                // Dodany kod do debugowania wartoœci formularza
                Console.WriteLine("?? WSZYSTKIE DANE FORMULARZA:");
                foreach (var key in Request.Form.Keys)
                {
                    if (key.Contains("Password", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"   Klucz: {key}, D³ugoœæ: {Request.Form[key].ToString().Length}");
                    }
                    else
                    {
                        Console.WriteLine($"   Klucz: {key}, Wartoœæ: {Request.Form[key]}");
                    }
                }

                // Sprawdzenie ModelState
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("? ModelState jest NIEPOPRAWNY!");
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"? B³¹d walidacji: {modelError.ErrorMessage}");
                    }
                    return Page();
                }

                // Wyszukanie u¿ytkownika
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == Username);

                if (user == null)
                {
                    ErrorMessage = "Nieprawid³owa nazwa u¿ytkownika lub has³o.";
                    Console.WriteLine("? Nie znaleziono u¿ytkownika.");
                    return Page();
                }

                // Weryfikacja has³a
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash);

                if (!isPasswordValid)
                {
                    ErrorMessage = "Nieprawid³owa nazwa u¿ytkownika lub has³o.";
                    Console.WriteLine("? Nieprawid³owe has³o.");
                    return Page();
                }

                // Tworzenie claim
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

                Console.WriteLine($"? U¿ytkownik {user.Username} zosta³ zalogowany.");

                // Przekierowanie do strony g³ównej
                return RedirectToPage("/Diet/Index");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Wyst¹pi³ b³¹d podczas logowania: " + ex.Message;
                Console.WriteLine("? B³¹d: " + ex.Message);
                Console.WriteLine("? Stack trace: " + ex.StackTrace);
                return Page();
            }
        }
    }
}