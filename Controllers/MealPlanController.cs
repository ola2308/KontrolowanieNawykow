using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KontrolaNawykow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MealPlanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MealPlanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/mealplan
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MealPlan>>> GetMealPlans()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                return await _context.MealPlans
                    .Where(mp => mp.UserId == userId)
                    .Include(mp => mp.Recipe)
                    .OrderBy(mp => mp.Date)
                    .ThenBy(mp => mp.MealType)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania planu posiłków: {ex.Message}");
            }
        }

        // GET: api/mealplan/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MealPlan>> GetMealPlan(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var mealPlan = await _context.MealPlans
                    .Include(mp => mp.Recipe)
                    .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

                if (mealPlan == null)
                {
                    return NotFound($"Nie znaleziono posiłku o ID {id}");
                }

                return mealPlan;
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas pobierania posiłku: {ex.Message}");
            }
        }

        // POST: api/mealplan
        [HttpPost]
        public async Task<ActionResult<MealPlan>> PostMealPlan([FromBody] MealPlanDto mealPlanDto)
        {
            try
            {
                if (mealPlanDto == null)
                {
                    return BadRequest("Brak danych posiłku");
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var mealPlan = new MealPlan
                {
                    UserId = userId,
                    Date = DateTime.Parse(mealPlanDto.Date),
                    MealType = (MealType)Enum.Parse(typeof(MealType), mealPlanDto.MealType),
                    RecipeId = mealPlanDto.RecipeId,
                    Eaten = false,
                    CustomEntry = mealPlanDto.CustomEntry ?? string.Empty
                };

                _context.MealPlans.Add(mealPlan);
                await _context.SaveChangesAsync();

                // Pobierz pełne dane posiłku wraz z przepisem
                var savedMealPlan = await _context.MealPlans
                    .Include(mp => mp.Recipe)
                    .FirstOrDefaultAsync(mp => mp.Id == mealPlan.Id);

                return CreatedAtAction(nameof(GetMealPlan), new { id = mealPlan.Id }, savedMealPlan);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas dodawania posiłku: {ex.Message}");
            }
        }

        // PUT: api/mealplan/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMealPlan(int id, [FromBody] MealPlanDto mealPlanDto)
        {
            try
            {
                if (mealPlanDto == null)
                {
                    return BadRequest("Brak danych posiłku");
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var mealPlan = await _context.MealPlans
                    .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

                if (mealPlan == null)
                {
                    return NotFound($"Nie znaleziono posiłku o ID {id}");
                }

                // Aktualizuj właściwości
                mealPlan.Date = DateTime.Parse(mealPlanDto.Date);
                mealPlan.MealType = (MealType)Enum.Parse(typeof(MealType), mealPlanDto.MealType);

                // Ważne: Aktualizuj RecipeId i CustomEntry
                mealPlan.RecipeId = mealPlanDto.RecipeId;
                mealPlan.CustomEntry = mealPlanDto.CustomEntry ?? mealPlan.CustomEntry;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MealPlanExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas aktualizacji posiłku: {ex.Message}");
            }
        }

        // PUT: api/mealplan/5/eaten
        [HttpPut("{id}/eaten")]
        public async Task<IActionResult> MarkMealEaten(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var mealPlan = await _context.MealPlans
                    .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

                if (mealPlan == null)
                {
                    return NotFound($"Nie znaleziono posiłku o ID {id}");
                }

                mealPlan.Eaten = true;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas oznaczania posiłku jako zjedzonego: {ex.Message}");
            }
        }

        // PUT: api/mealplan/5/uneaten
        [HttpPut("{id}/uneaten")]
        public async Task<IActionResult> UnmarkMealEaten(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var mealPlan = await _context.MealPlans
                    .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

                if (mealPlan == null)
                {
                    return NotFound($"Nie znaleziono posiłku o ID {id}");
                }

                mealPlan.Eaten = false;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas oznaczania posiłku jako niezjedzonego: {ex.Message}");
            }
        }

        // DELETE: api/mealplan/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMealPlan(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var mealPlan = await _context.MealPlans
                    .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

                if (mealPlan == null)
                {
                    return NotFound($"Nie znaleziono posiłku o ID {id}");
                }

                _context.MealPlans.Remove(mealPlan);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas usuwania posiłku: {ex.Message}");
            }
        }

        private bool MealPlanExists(int id)
        {
            return _context.MealPlans.Any(e => e.Id == id);
        }
    }

    public class MealPlanDto
    {
        public string Date { get; set; }
        public string MealType { get; set; }
        public int? RecipeId { get; set; }
        public string CustomEntry { get; set; }
    }
}