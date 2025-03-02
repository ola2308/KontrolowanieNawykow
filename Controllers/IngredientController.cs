using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace KontrolaNawykow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IngredientController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public IngredientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ingredient
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredients()
        {
            return await _context.Ingredients
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        // GET: api/ingredient/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Ingredient>> GetIngredient(int id)
        {
            var ingredient = await _context.Ingredients
                .FirstOrDefaultAsync(i => i.Id == id);

            if (ingredient == null)
            {
                return NotFound();
            }

            return ingredient;
        }

        // POST: api/ingredient
        [HttpPost]
        public async Task<ActionResult<Ingredient>> PostIngredient(IngredientDto ingredientDto)
        {
            var ingredient = new Ingredient
            {
                Name = ingredientDto.Name,
                Calories = ingredientDto.Calories,
                Protein = ingredientDto.Protein,
                Fat = ingredientDto.Fat,
                Carbs = ingredientDto.Carbs
            };

            _context.Ingredients.Add(ingredient);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetIngredient), new { id = ingredient.Id }, ingredient);
        }

        // PUT: api/ingredient/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutIngredient(int id, IngredientDto ingredientDto)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);

            if (ingredient == null)
            {
                return NotFound();
            }

            // Aktualizuj właściwości
            ingredient.Name = ingredientDto.Name;
            ingredient.Calories = ingredientDto.Calories;
            ingredient.Protein = ingredientDto.Protein;
            ingredient.Fat = ingredientDto.Fat;
            ingredient.Carbs = ingredientDto.Carbs;

            // Zapisz zmiany
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!IngredientExists(id))
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

        // DELETE: api/ingredient/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
            {
                return NotFound();
            }

            // Sprawdź, czy składnik jest używany w przepisach
            bool isUsed = await _context.RecipeIngredients.AnyAsync(ri => ri.IngredientId == id);
            if (isUsed)
            {
                return BadRequest("Nie można usunąć składnika, który jest używany w przepisach.");
            }

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool IngredientExists(int id)
        {
            return _context.Ingredients.Any(e => e.Id == id);
        }
    }

    public class IngredientDto
    {
        public string Name { get; set; }
        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Fat { get; set; }
        public float Carbs { get; set; }
    }
}