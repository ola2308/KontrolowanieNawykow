using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using KontrolaNawykow.Models;

var builder = WebApplication.CreateBuilder(args);

// 📌 1️⃣ Dodanie kontekstu bazy danych z logowaniem
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("❌ BŁĄD: Brak ConnectionString w pliku konfiguracji!");
    }
    Console.WriteLine($"✅ Połączono z bazą danych: {connectionString}");
    options.UseSqlServer(connectionString);
});

// 📌 2️⃣ Konfiguracja uwierzytelniania cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "KontrolaNawykowAuth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        Console.WriteLine("✅ Konfiguracja uwierzytelniania cookie załadowana.");
    });

// 📌 3️⃣ Konfiguracja sesji
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    Console.WriteLine("✅ Sesja została skonfigurowana.");
});

// 📌 4️⃣ Dodanie MVC i Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
Console.WriteLine("✅ MVC i Razor Pages załadowane.");

var app = builder.Build();

// 📌 5️⃣ Konfiguracja błędów w trybie produkcyjnym
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    Console.WriteLine("✅ Tryb produkcyjny - używam HSTS.");
}
else
{
    Console.WriteLine("✅ Tryb deweloperski - HSTS wyłączone.");
}

// 📌 6️⃣ Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
Console.WriteLine("✅ Middleware został skonfigurowany.");

// Uwierzytelnianie i autoryzacja
app.UseAuthentication();
app.UseAuthorization();
Console.WriteLine("✅ Middleware uwierzytelniania i autoryzacji dodany.");

// Sesja
app.UseSession();
Console.WriteLine("✅ Middleware sesji dodany.");

// Najpierw mapuj konkretne trasy do kontrolera
// Najpierw Razor Pages
app.MapRazorPages();

// Potem kontrolery MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

Console.WriteLine("✅ Mapowanie tras zakończone. Aplikacja startuje...");

// Uruchomienie aplikacji
app.Run();
