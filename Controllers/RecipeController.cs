using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace KontrolaNawykow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RecipeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RecipeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/recipe
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Recipe>>> GetRecipes()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return await _context.Recipes
                .Where(r => r.UserId == userId || r.IsPublic)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .ToListAsync();
        }

        // GET: api/recipe/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Recipe>> GetRecipe(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == id && (r.UserId == userId || r.IsPublic));

            if (recipe == null)
            {
                return NotFound();
            }

            return recipe;
        }

        // POST: api/recipe
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Recipe>> PostRecipe([FromForm] RecipeDto recipeDto, IFormFile image)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var recipe = new Recipe
                {
                    Name = recipeDto.Name,
                    Instructions = recipeDto.Instructions,
                    Calories = recipeDto.Calories,
                    Protein = recipeDto.Protein,
                    Fat = recipeDto.Fat,
                    Carbs = recipeDto.Carbs,
                    IsPublic = recipeDto.IsPublic,
                    UserId = userId
                };

                // Obsługa obrazu
                if (image != null && image.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await image.CopyToAsync(memoryStream);
                        recipe.ImageData = memoryStream.ToArray();
                    }
                }

                // Walidacja składników
                List<RecipeIngredientDto> ingredientsData = new();
                if (!string.IsNullOrEmpty(recipeDto.RecipeIngredients))
                {
                    try
                    {
                        ingredientsData = System.Text.Json.JsonSerializer.Deserialize<List<RecipeIngredientDto>>(recipeDto.RecipeIngredients);

                        if (ingredientsData == null || !ingredientsData.Any())
                        {
                            return BadRequest("Lista składników jest pusta lub nieprawidłowa.");
                        }

                        if (ingredientsData.Any(ri => ri.IngredientId <= 0 || ri.Amount <= 0))
                        {
                            return BadRequest("Każdy składnik musi mieć prawidłowe ID i ilość większą od 0.");
                        }

                        var existingIngredientIds = _context.Ingredients.Select(i => i.Id).ToHashSet();
                        if (!ingredientsData.All(ri => existingIngredientIds.Contains(ri.IngredientId)))
                        {
                            return BadRequest("Niektóre składniki nie istnieją w bazie danych.");
                        }
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        return BadRequest($"Błąd deserializacji składników: {ex.Message}");
                    }
                }
                else
                {
                    return BadRequest("Przepis musi zawierać przynajmniej jeden składnik.");
                }

                // Przypisanie składników do przepisu
                recipe.RecipeIngredients = ingredientsData.Select(ri => new RecipeIngredient
                {
                    IngredientId = ri.IngredientId,
                    Amount = ri.Amount
                }).ToList();

                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, recipe);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wystąpił błąd serwera: {ex.Message}");
            }
        }

        // PUT: api/recipe/5
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PutRecipe(int id, [FromForm] RecipeDto recipeDto, IFormFile image)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var existingRecipe = await _context.Recipes
                    .Include(r => r.RecipeIngredients)
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

                if (existingRecipe == null)
                {
                    return NotFound();
                }

                // Aktualizacja danych
                existingRecipe.Name = recipeDto.Name;
                existingRecipe.Calories = recipeDto.Calories;
                existingRecipe.Protein = recipeDto.Protein;
                existingRecipe.Fat = recipeDto.Fat;
                existingRecipe.Carbs = recipeDto.Carbs;
                existingRecipe.Instructions = recipeDto.Instructions;
                existingRecipe.IsPublic = recipeDto.IsPublic;

                // Aktualizacja obrazu
                if (image != null && image.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await image.CopyToAsync(memoryStream);
                        existingRecipe.ImageData = memoryStream.ToArray();
                    }
                }

                // Aktualizacja składników
                List<RecipeIngredientDto> ingredientsData = new();
                if (!string.IsNullOrEmpty(recipeDto.RecipeIngredients))
                {
                    try
                    {
                        ingredientsData = System.Text.Json.JsonSerializer.Deserialize<List<RecipeIngredientDto>>(recipeDto.RecipeIngredients);

                        if (ingredientsData.Any(ri => ri.IngredientId <= 0 || ri.Amount <= 0))
                        {
                            return BadRequest("Każdy składnik musi mieć prawidłowe ID i ilość większą od 0.");
                        }
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        return BadRequest($"Błąd deserializacji składników: {ex.Message}");
                    }
                }

                // Usuń stare składniki i dodaj nowe
                _context.RecipeIngredients.RemoveRange(existingRecipe.RecipeIngredients);
                existingRecipe.RecipeIngredients = ingredientsData.Select(ri => new RecipeIngredient
                {
                    RecipeId = id,
                    IngredientId = ri.IngredientId,
                    Amount = ri.Amount
                }).ToList();

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wystąpił błąd serwera: {ex.Message}");
            }
        }

        // DELETE: api/recipe/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (recipe == null)
            {
                return NotFound();
            }

            _context.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class RecipeDto
    {
        public string Name { get; set; }
        public string Instructions { get; set; }
        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Fat { get; set; }
        public float Carbs { get; set; }
        public bool IsPublic { get; set; }
        public string RecipeIngredients { get; set; }
    }

    public class RecipeIngredientDto
    {
        public int IngredientId { get; set; }
        public float Amount { get; set; }
    }
}
