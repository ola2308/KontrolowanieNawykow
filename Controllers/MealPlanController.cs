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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return await _context.MealPlans
                .Where(mp => mp.UserId == userId)
                .Include(mp => mp.Recipe)
                .OrderBy(mp => mp.Date)
                .ThenBy(mp => mp.MealType)
                .ToListAsync();
        }

        // GET: api/mealplan/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MealPlan>> GetMealPlan(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var mealPlan = await _context.MealPlans
                .Include(mp => mp.Recipe)
                .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

            if (mealPlan == null)
            {
                return NotFound();
            }

            return mealPlan;
        }

        // POST: api/mealplan
        [HttpPost]
        public async Task<ActionResult<MealPlan>> PostMealPlan([FromBody] MealPlanDto mealPlanDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var mealPlan = new MealPlan
            {
                UserId = userId,
                Date = DateTime.Parse(mealPlanDto.Date),
                MealType = (MealType)Enum.Parse(typeof(MealType), mealPlanDto.MealType),
                RecipeId = mealPlanDto.RecipeId,
                Eaten = false
            };

            _context.MealPlans.Add(mealPlan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMealPlan), new { id = mealPlan.Id }, mealPlan);
        }

        // PUT: api/mealplan/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMealPlan(int id, [FromBody] MealPlanDto mealPlanDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var mealPlan = await _context.MealPlans.FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

            if (mealPlan == null)
            {
                return NotFound();
            }

            // Aktualizuj właściwości
            mealPlan.Date = DateTime.Parse(mealPlanDto.Date);
            mealPlan.MealType = (MealType)Enum.Parse(typeof(MealType), mealPlanDto.MealType);
            mealPlan.RecipeId = mealPlanDto.RecipeId;

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

        // PUT: api/mealplan/5/eaten
        [HttpPut("{id}/eaten")]
        public async Task<IActionResult> MarkMealEaten(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var mealPlan = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

            if (mealPlan == null)
            {
                return NotFound();
            }

            mealPlan.Eaten = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/mealplan/5/uneaten
        [HttpPut("{id}/uneaten")]
        public async Task<IActionResult> UnmarkMealEaten(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var mealPlan = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

            if (mealPlan == null)
            {
                return NotFound();
            }

            mealPlan.Eaten = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/mealplan/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMealPlan(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var mealPlan = await _context.MealPlans
                .FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId);

            if (mealPlan == null)
            {
                return NotFound();
            }

            _context.MealPlans.Remove(mealPlan);
            await _context.SaveChangesAsync();

            return NoContent();
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
    }
}