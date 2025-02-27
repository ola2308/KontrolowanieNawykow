using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;

namespace KontrolaNawykow.Pages.Account
{
    [AllowAnonymous] // Upewniamy siê, ¿e dostêp nie jest blokowany przez autoryzacjê
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Nazwa u¿ytkownika jest wymagana")]
        [StringLength(50, ErrorMessage = "Nazwa u¿ytkownika musi mieæ od {2} do {1} znaków.", MinimumLength = 3)]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawid³owy format adresu email")]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Has³o jest wymagane")]
        [StringLength(100, ErrorMessage = "Has³o musi mieæ minimum {2} znaków.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Potwierdzenie has³a jest wymagane")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Has³a nie s¹ identyczne")]
        public string ConfirmPassword { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
            Console.WriteLine("? Otworzono stronê rejestracji.");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Console.WriteLine("?? Próba rejestracji - pocz¹tek metody OnPostAsync");

                // Rêcznie pobierz wartoœci z formularza
                string username = Request.Form["Username"].ToString();
                string email = Request.Form["Email"].ToString();
                string password = Request.Form["Password"].ToString();
                string confirmPassword = Request.Form["ConfirmPassword"].ToString();

                Console.WriteLine($"?? Rêcznie pobrane wartoœci: Username='{username}', Email='{email}', Password length={password.Length}, ConfirmPassword length={confirmPassword.Length}");

                // Przypisz wartoœci do w³aœciwoœci modelu
                Username = username;
                Email = email;
                Password = password;
                ConfirmPassword = confirmPassword;

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

                // 2?? Sprawdzenie ModelState
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("? ModelState jest NIEPOPRAWNY!");
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"? B³¹d walidacji: {modelError.ErrorMessage}");
                    }
                    return Page();
                }

                // 3?? Sprawdzenie, czy u¿ytkownik istnieje
                if (await _context.Users.AnyAsync(u => u.Username == Username))
                {
                    ErrorMessage = "U¿ytkownik o takiej nazwie ju¿ istnieje.";
                    Console.WriteLine("? U¿ytkownik o takiej nazwie ju¿ istnieje.");
                    return Page();
                }

                if (await _context.Users.AnyAsync(u => u.Email == Email))
                {
                    ErrorMessage = "Ten adres email jest ju¿ u¿ywany.";
                    Console.WriteLine("? Adres email ju¿ istnieje w bazie.");
                    return Page();
                }

                // Bezpoœrednie utworzenie u¿ytkownika z podstawowymi danymi
                var user = new User
                {
                    Username = Username,
                    Email = Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                    CreatedAt = DateTime.UtcNow
                };

                // Dodanie u¿ytkownika do bazy danych
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                Console.WriteLine($"? Utworzono u¿ytkownika z ID: {user.Id}");

                // Zapisanie ID u¿ytkownika w TempData
                TempData["UserId"] = user.Id;

                Console.WriteLine($"? Dane zapisane w TempData: UserId={TempData["UserId"]}");

                // Upewnienie siê, ¿e TempData zostaje zachowane
                TempData.Keep("UserId");

                Console.WriteLine("? Przekierowanie do RegisterProfile...");
                return RedirectToPage("/Account/RegisterProfile");
            }
            catch (Exception ex)
            {
                // 6?? Obs³uga b³êdów
                ErrorMessage = "Wyst¹pi³ b³¹d podczas przetwarzania danych: " + ex.Message;
                Console.WriteLine("? B³¹d: " + ex.Message);
                Console.WriteLine("? Stack trace: " + ex.StackTrace);
                return Page();
            }
        }
    }
}