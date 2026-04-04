using ExperimentSimulation.DataAccessLayer.Concrete;
using ExperimentSimulation.EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExperimentSimulation.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ContentCreator,İçerik Üreticisi,Icerik Ureticisi")]
    public class TodoController : ControllerBase
    {
        private readonly Context _context;

        public TodoController(Context context)
        {
            _context = context;
        }

        public class TodoSubtaskDto
        {
            public int id { get; set; }
            public string title { get; set; } = string.Empty;
            public bool isCompleted { get; set; }
        }

        public class TodoItemDto
        {
            public int id { get; set; }
            public string title { get; set; } = string.Empty;
            public string priority { get; set; } = "Orta";
            public string dueDate { get; set; } = string.Empty;
            public string description { get; set; } = string.Empty;
            public string notes { get; set; } = string.Empty;
            public bool isCompleted { get; set; }
            public string createdAtUtc { get; set; } = string.Empty;
            public string updatedAtUtc { get; set; } = string.Empty;
            public TodoSubtaskDto[] subtasks { get; set; } = Array.Empty<TodoSubtaskDto>();
        }

        public class UpsertTodoItemDto
        {
            public string title { get; set; } = string.Empty;
            public string priority { get; set; } = "Orta";
            public string dueDate { get; set; } = string.Empty;
            public string description { get; set; } = string.Empty;
            public string notes { get; set; } = string.Empty;
            public bool isCompleted { get; set; }
        }

        public class CreateTodoSubtaskDto
        {
            public string title { get; set; } = string.Empty;
        }

        public class UpdateTodoSubtaskDto
        {
            public string title { get; set; } = string.Empty;
            public bool isCompleted { get; set; }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMine()
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(new { message = "Kullanıcı doğrulanamadı." });

            var todos = await _context.TodoItems
                .AsNoTracking()
                .Include(t => t.Subtasks)
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.UpdatedAtUtc)
                .ToListAsync();

            return Ok(todos.Select(ToDto));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertTodoItemDto dto)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(new { message = "Kullanıcı doğrulanamadı." });

            var normalized = NormalizeTodoPayload(dto);
            if (!normalized.success)
                return BadRequest(new { message = normalized.error });

            var now = DateTime.UtcNow;
            var entity = new TodoItem
            {
                UserId = userId,
                Title = normalized.title,
                Priority = normalized.priority,
                DueDate = normalized.dueDate,
                Description = normalized.description,
                Notes = normalized.notes,
                IsCompleted = normalized.isCompleted,
                CompletedAtUtc = normalized.isCompleted ? now : null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _context.TodoItems.Add(entity);
            await _context.SaveChangesAsync();

            var persisted = await _context.TodoItems
                .AsNoTracking()
                .Include(t => t.Subtasks)
                .FirstOrDefaultAsync(t => t.Id == entity.Id);

            if (persisted == null)
                return StatusCode(500, new { message = "Kayıt doğrulanamadı." });

            return Ok(ToDto(persisted));
        }

        [HttpPut("{todoId:int}")]
        public async Task<IActionResult> Update(int todoId, [FromBody] UpsertTodoItemDto dto)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(new { message = "Kullanıcı doğrulanamadı." });

            var entity = await _context.TodoItems
                .Include(t => t.Subtasks)
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (entity == null)
                return NotFound(new { message = "ToDo kaydı bulunamadı." });

            var normalized = NormalizeTodoPayload(dto);
            if (!normalized.success)
                return BadRequest(new { message = normalized.error });

            entity.Title = normalized.title;
            entity.Priority = normalized.priority;
            entity.DueDate = normalized.dueDate;
            entity.Description = normalized.description;
            entity.Notes = normalized.notes;
            entity.IsCompleted = normalized.isCompleted;
            entity.CompletedAtUtc = normalized.isCompleted ? DateTime.UtcNow : null;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(ToDto(entity));
        }

        [HttpDelete("{todoId:int}")]
        public async Task<IActionResult> Delete(int todoId)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(new { message = "Kullanıcı doğrulanamadı." });

            var entity = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (entity == null)
                return NotFound(new { message = "ToDo kaydı bulunamadı." });

            _context.TodoItems.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { message = "ToDo silindi." });
        }

        [HttpPost("{todoId:int}/subtasks")]
        public async Task<IActionResult> AddSubtask(int todoId, [FromBody] CreateTodoSubtaskDto dto)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(new { message = "Kullanıcı doğrulanamadı." });

            var parent = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (parent == null)
                return NotFound(new { message = "ToDo kaydı bulunamadı." });

            string title = (dto.title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest(new { message = "Alt görev metni zorunlu." });

            var now = DateTime.UtcNow;
            var subtask = new TodoSubtask
            {
                TodoItemId = todoId,
                Title = title,
                IsCompleted = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _context.TodoSubtasks.Add(subtask);
            parent.UpdatedAtUtc = now;
            await _context.SaveChangesAsync();

            return Ok(new TodoSubtaskDto
            {
                id = subtask.Id,
                title = subtask.Title,
                isCompleted = subtask.IsCompleted
            });
        }

        [HttpPut("{todoId:int}/subtasks/{subtaskId:int}")]
        public async Task<IActionResult> UpdateSubtask(int todoId, int subtaskId, [FromBody] UpdateTodoSubtaskDto dto)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(new { message = "Kullanıcı doğrulanamadı." });

            if (dto == null)
                return BadRequest(new { message = "Geçersiz istek." });

            var parent = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (parent == null)
                return NotFound(new { message = "ToDo kaydı bulunamadı." });

            var subtask = await _context.TodoSubtasks
                .FirstOrDefaultAsync(s => s.Id == subtaskId && s.TodoItemId == todoId);

            if (subtask == null)
                return NotFound(new { message = "Alt görev bulunamadı." });

            string title = (dto?.title ?? string.Empty).Trim();
            bool isCompleted = dto != null && dto.isCompleted;
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest(new { message = "Alt görev metni zorunlu." });

            subtask.Title = title;
            subtask.IsCompleted = isCompleted;
            subtask.UpdatedAtUtc = DateTime.UtcNow;
            parent.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new TodoSubtaskDto
            {
                id = subtask.Id,
                title = subtask.Title,
                isCompleted = subtask.IsCompleted
            });
        }

        [HttpDelete("{todoId:int}/subtasks/{subtaskId:int}")]
        public async Task<IActionResult> DeleteSubtask(int todoId, int subtaskId)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(new { message = "Kullanıcı doğrulanamadı." });

            var parent = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);

            if (parent == null)
                return NotFound(new { message = "ToDo kaydı bulunamadı." });

            var subtask = await _context.TodoSubtasks
                .FirstOrDefaultAsync(s => s.Id == subtaskId && s.TodoItemId == todoId);

            if (subtask == null)
                return NotFound(new { message = "Alt görev bulunamadı." });

            _context.TodoSubtasks.Remove(subtask);
            parent.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Alt görev silindi." });
        }

        private static (bool success, string error, string title, string priority, DateTime dueDate, string description, string notes, bool isCompleted) NormalizeTodoPayload(UpsertTodoItemDto dto)
        {
            if (dto == null)
                return (false, "Geçersiz istek.", string.Empty, string.Empty, DateTime.MinValue, string.Empty, string.Empty, false);

            string title = (dto.title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(title))
                return (false, "Başlık zorunlu.", string.Empty, string.Empty, DateTime.MinValue, string.Empty, string.Empty, false);

            if (!DateTime.TryParse(dto.dueDate, out var dueDate))
                return (false, "Teslim tarihi geçersiz.", string.Empty, string.Empty, DateTime.MinValue, string.Empty, string.Empty, false);

            string priority = NormalizePriority(dto.priority);
            string description = (dto.description ?? string.Empty).Trim();
            string notes = (dto.notes ?? string.Empty).Trim();

            return (true, string.Empty, title, priority, dueDate.Date, description, notes, dto.isCompleted);
        }

        private static string NormalizePriority(string raw)
        {
            string input = (raw ?? string.Empty).Trim().ToLowerInvariant();
            if (input.Contains("yük") || input.Contains("yuk") || input.Contains("high"))
                return "Yüksek";
            if (input.Contains("düş") || input.Contains("dus") || input.Contains("low"))
                return "Düşük";
            return "Orta";
        }

        private static TodoItemDto ToDto(TodoItem entity)
        {
            return new TodoItemDto
            {
                id = entity.Id,
                title = entity.Title,
                priority = entity.Priority,
                dueDate = entity.DueDate.ToString("yyyy-MM-dd"),
                description = entity.Description,
                notes = entity.Notes,
                isCompleted = entity.IsCompleted,
                createdAtUtc = entity.CreatedAtUtc.ToString("O"),
                updatedAtUtc = entity.UpdatedAtUtc.ToString("O"),
                subtasks = (entity.Subtasks ?? new List<TodoSubtask>())
                    .OrderBy(s => s.CreatedAtUtc)
                    .Select(s => new TodoSubtaskDto
                    {
                        id = s.Id,
                        title = s.Title,
                        isCompleted = s.IsCompleted
                    })
                    .ToArray()
            };
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            if (int.TryParse(idClaim, out int id))
                return id;
            return 0;
        }
    }
}
