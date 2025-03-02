using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace KontrolaNawyk�w.Pages.Account
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
        [Required(ErrorMessage = "Nazwa u�ytkownika jest wymagana")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Has�o jest wymagane")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public void OnGet()
        {
            // Je�li zostali�my przekierowani z pomy�lnej rejestracji
            if (TempData["RegisterSuccess"] != null)
            {
                SuccessMessage = TempData["RegisterSuccess"].ToString();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Console.WriteLine("?? Pr�ba logowania - pocz�tek metody OnPostAsync");

                // R�cznie pobierz warto�ci z formularza
                string username = Request.Form["Username"].ToString();
                string password = Request.Form["Password"].ToString();

                Console.WriteLine($"?? R�cznie pobrane warto�ci: Username='{username}', Password length={password.Length}");

                // Przypisz warto�ci do w�a�ciwo�ci modelu
                Username = username;
                Password = password;

                // Dodany kod do debugowania warto�ci formularza
                Console.WriteLine("?? WSZYSTKIE DANE FORMULARZA:");
                foreach (var key in Request.Form.Keys)
                {
                    if (key.Contains("Password", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"   Klucz: {key}, D�ugo��: {Request.Form[key].ToString().Length}");
                    }
                    else
                    {
                        Console.WriteLine($"   Klucz: {key}, Warto��: {Request.Form[key]}");
                    }
                }

                // Sprawdzenie ModelState
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("? ModelState jest NIEPOPRAWNY!");
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"? B��d walidacji: {modelError.ErrorMessage}");
                    }
                    return Page();
                }

                // Wyszukanie u�ytkownika
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == Username);

                if (user == null)
                {
                    ErrorMessage = "Nieprawid�owa nazwa u�ytkownika lub has�o.";
                    Console.WriteLine("? Nie znaleziono u�ytkownika.");
                    return Page();
                }

                // Weryfikacja has�a
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash);

                if (!isPasswordValid)
                {
                    ErrorMessage = "Nieprawid�owa nazwa u�ytkownika lub has�o.";
                    Console.WriteLine("? Nieprawid�owe has�o.");
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

                Console.WriteLine($"? U�ytkownik {user.Username} zosta� zalogowany.");

                // Przekierowanie do strony g��wnej
                return RedirectToPage("/Diet/Index");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Wyst�pi� b��d podczas logowania: " + ex.Message;
                Console.WriteLine("? B��d: " + ex.Message);
                Console.WriteLine("? Stack trace: " + ex.StackTrace);
                return Page();
            }
        }
    }
}