using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KontrolaNawykow.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace KontrolaNawykow.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TodoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/todo
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ToDo>>> GetTodos()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            return await _context.ToDos
                .Where(t => t.UserId == userId && !t.IsTemplate) // Wykluczamy szablony
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }

        // GET: api/todo/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ToDo>> GetTodo(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var todo = await _context.ToDos
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
            {
                return NotFound();
            }

            return todo;
        }

        // GET: api/todo/saved-activities
        [HttpGet("saved-activities")]
        public async Task<ActionResult<IEnumerable<ToDo>>> GetSavedActivities()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var savedActivities = await _context.ToDos
                .Where(t => t.UserId == userId && t.IsTemplate == true)
                .OrderBy(t => t.Task)
                .ToListAsync();

            return savedActivities;
        }

        // POST: api/todo
        [HttpPost]
        public async Task<ActionResult<ToDo>> PostTodo([FromBody] TodoDto todoDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var todo = new ToDo
            {
                UserId = userId,
                Task = todoDto.Task,
                IsCompleted = false,
                CreatedAt = DateTime.Parse(todoDto.Date),
                IsTemplate = false // Upewniamy się, że to nie jest szablon
            };

            _context.ToDos.Add(todo);
            await _context.SaveChangesAsync();

            // Jeśli ustawiono flagę saveAsTemplate, zapisz również jako szablon
            if (todoDto.SaveAsTemplate)
            {
                // Sprawdź czy już istnieje szablon o tej samej nazwie
                var existingTemplate = await _context.ToDos
                    .AnyAsync(t => t.UserId == userId && t.IsTemplate && t.Task == todoDto.Task);

                // Jeśli nie istnieje, dodaj nowy szablon
                if (!existingTemplate)
                {
                    var template = new ToDo
                    {
                        UserId = userId,
                        Task = todoDto.Task,
                        IsCompleted = false,
                        CreatedAt = DateTime.UtcNow,
                        IsTemplate = true
                    };

                    _context.ToDos.Add(template);
                    await _context.SaveChangesAsync();
                }
            }

            return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, todo);
        }

        // PUT: api/todo/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTodo(int id, [FromBody] TodoDto todoDto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var todo = await _context.ToDos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
            {
                return NotFound();
            }

            // Aktualizuj właściwości
            todo.Task = todoDto.Task;
            if (!string.IsNullOrEmpty(todoDto.Date))
            {
                todo.CreatedAt = DateTime.Parse(todoDto.Date);
            }

            // Zapisz zmiany
            try
            {
                await _context.SaveChangesAsync();

                // Jeśli ustawiono flagę saveAsTemplate, dodaj jako szablon
                if (todoDto.SaveAsTemplate)
                {
                    // Sprawdź czy już istnieje szablon o tej samej nazwie
                    var existingTemplate = await _context.ToDos
                        .FirstOrDefaultAsync(t => t.UserId == userId && t.IsTemplate && t.Task == todoDto.Task);

                    if (existingTemplate == null)
                    {
                        var template = new ToDo
                        {
                            UserId = userId,
                            Task = todoDto.Task,
                            IsCompleted = false,
                            CreatedAt = DateTime.UtcNow,
                            IsTemplate = true
                        };

                        _context.ToDos.Add(template);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TodoExists(id))
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

        // PUT: api/todo/5/complete
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> MarkTodoCompleted(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var todo = await _context.ToDos
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
            {
                return NotFound();
            }

            todo.IsCompleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/todo/5/uncomplete
        [HttpPut("{id}/uncomplete")]
        public async Task<IActionResult> UnmarkTodoCompleted(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var todo = await _context.ToDos
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
            {
                return NotFound();
            }

            todo.IsCompleted = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/todo/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var todo = await _context.ToDos
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
            {
                return NotFound();
            }

            _context.ToDos.Remove(todo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/todo/saved-activities/5
        [HttpDelete("saved-activities/{id}")]
        public async Task<IActionResult> DeleteSavedActivity(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var template = await _context.ToDos
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.IsTemplate);

            if (template == null)
            {
                return NotFound();
            }

            _context.ToDos.Remove(template);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TodoExists(int id)
        {
            return _context.ToDos.Any(t => t.Id == id);
        }
    }

    public class TodoDto
    {
        public string Date { get; set; }
        public string Task { get; set; }
        public bool SaveAsTemplate { get; set; } = false;
    }
}