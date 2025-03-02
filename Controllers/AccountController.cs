using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using Microsoft.AspNetCore.Authorization;
using System;

namespace KontrolaNawykow.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Jeśli użytkownik jest już zalogowany, przekieruj go do strony głównej
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Diet/Index");
            }
            // Przekierowanie do strony Razor Page logowania
            return RedirectToPage("/Account/Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            // Jeśli użytkownik jest już zalogowany, przekieruj go do strony głównej
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }
            // Przekierowanie do strony Razor Page rejestracji
            return RedirectToPage("/Account/Register");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Account/Login");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == User.Identity.Name);

            if (user == null)
            {
                return NotFound();
            }

            // Przekierowanie do strony Razor Page profilu
            return RedirectToPage("/Profile/Setup");
        }
    }
}