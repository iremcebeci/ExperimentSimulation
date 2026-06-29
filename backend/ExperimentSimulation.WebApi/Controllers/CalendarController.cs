using ExperimentSimulation.DataAccessLayer.Concrete;
using ExperimentSimulation.EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace ExperimentSimulation.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CalendarController : ControllerBase
    {
        private readonly Context _context;

        public CalendarController(Context context)
        {
            _context = context;
        }

        public class CalendarCategoryDto
        {
            public int Id { get; set; }
            public string Type { get; set; } = null!;
            public string Label { get; set; } = null!;
            public string Color { get; set; } = null!;
            public string TextColor { get; set; } = null!;
        }

        public class CreateCalendarCategoryDto
        {
            public string Label { get; set; } = null!;
            public string? Color { get; set; }
            public string? TextColor { get; set; }
        }

        public class CalendarEventDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = null!;
            public string Type { get; set; } = null!;
            public string Date { get; set; } = null!;
            public string Start { get; set; } = null!;
            public string End { get; set; } = null!;
            public string Location { get; set; } = string.Empty;
            public string RelatedClass { get; set; } = string.Empty;
            public string Desc { get; set; } = string.Empty;
        }

        public class UpsertCalendarEventDto
        {
            public string Title { get; set; } = null!;
            public string Type { get; set; } = null!;
            public string Date { get; set; } = null!;
            public string Start { get; set; } = null!;
            public string End { get; set; } = null!;
            public string? Location { get; set; }
            public string? RelatedClass { get; set; }
            public string? Desc { get; set; }
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            var items = await _context.CalendarCategories
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Label)
                .Select(x => new CalendarCategoryDto
                {
                    Id = x.Id,
                    Type = x.Type,
                    Label = x.Label,
                    Color = x.Color,
                    TextColor = x.TextColor
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCalendarCategoryDto dto)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Label))
                return BadRequest(new { message = "Kategori adı zorunlu." });

            string label = dto.Label.Trim();
            string color = NormalizeColor(dto.Color);
            string textColor = NormalizeTextColor(dto.TextColor, color);
            string baseType = MakeTypeKey(label);
            string type = baseType;
            int suffix = 2;

            while (await _context.CalendarCategories.AnyAsync(x => x.UserId == userId && x.Type == type))
            {
                type = baseType + "-" + suffix;
                suffix++;
            }

            var entity = new CalendarCategory
            {
                UserId = userId,
                Type = type,
                Label = label,
                Color = color,
                TextColor = textColor,
                CreatedAt = DateTime.UtcNow
            };

            _context.CalendarCategories.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new CalendarCategoryDto
            {
                Id = entity.Id,
                Type = entity.Type,
                Label = entity.Label,
                Color = entity.Color,
                TextColor = entity.TextColor
            });
        }

        [HttpGet("events")]
        public async Task<IActionResult> GetEvents([FromQuery] int? year = null, [FromQuery] int? month = null)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            var query = _context.CalendarEvents
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.UserId == userId);

            if (year.HasValue && month.HasValue && month.Value >= 1 && month.Value <= 12)
            {
                var rangeStart = new DateTime(year.Value, month.Value, 1);
                var rangeEnd = rangeStart.AddMonths(1);
                query = query.Where(x => x.Date >= rangeStart && x.Date < rangeEnd);
            }

            var items = await query
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Start)
                .Select(x => new CalendarEventDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Type = x.Category.Type,
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    Start = x.Start,
                    End = x.End,
                    Location = x.Location ?? string.Empty,
                    RelatedClass = x.RelatedClass ?? string.Empty,
                    Desc = x.Desc ?? string.Empty
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("events")]
        public async Task<IActionResult> CreateEvent([FromBody] UpsertCalendarEventDto dto)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (!TryValidateEventDto(dto, out DateTime parsedDate, out string message))
                return BadRequest(new { message });

            var category = await _context.CalendarCategories
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Type == dto.Type);

            if (category == null)
                return BadRequest(new { message = "Geçerli bir kategori seçilmelidir." });

            var entity = new CalendarEvent
            {
                UserId = userId,
                CategoryId = category.Id,
                Title = dto.Title.Trim(),
                Date = parsedDate,
                Start = dto.Start.Trim(),
                End = dto.End.Trim(),
                Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim(),
                RelatedClass = string.IsNullOrWhiteSpace(dto.RelatedClass) ? null : dto.RelatedClass.Trim(),
                Desc = string.IsNullOrWhiteSpace(dto.Desc) ? null : dto.Desc.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.CalendarEvents.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new CalendarEventDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Type = category.Type,
                Date = entity.Date.ToString("yyyy-MM-dd"),
                Start = entity.Start,
                End = entity.End,
                Location = entity.Location ?? string.Empty,
                RelatedClass = entity.RelatedClass ?? string.Empty,
                Desc = entity.Desc ?? string.Empty
            });
        }

        [HttpPut("events/{eventId:int}")]
        public async Task<IActionResult> UpdateEvent([FromRoute] int eventId, [FromBody] UpsertCalendarEventDto dto)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (!TryValidateEventDto(dto, out DateTime parsedDate, out string message))
                return BadRequest(new { message });

            var entity = await _context.CalendarEvents
                .FirstOrDefaultAsync(x => x.Id == eventId && x.UserId == userId);

            if (entity == null)
                return NotFound(new { message = "Etkinlik bulunamadı." });

            var category = await _context.CalendarCategories
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Type == dto.Type);

            if (category == null)
                return BadRequest(new { message = "Geçerli bir kategori seçilmelidir." });

            entity.CategoryId = category.Id;
            entity.Title = dto.Title.Trim();
            entity.Date = parsedDate;
            entity.Start = dto.Start.Trim();
            entity.End = dto.End.Trim();
            entity.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
            entity.RelatedClass = string.IsNullOrWhiteSpace(dto.RelatedClass) ? null : dto.RelatedClass.Trim();
            entity.Desc = string.IsNullOrWhiteSpace(dto.Desc) ? null : dto.Desc.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new CalendarEventDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Type = category.Type,
                Date = entity.Date.ToString("yyyy-MM-dd"),
                Start = entity.Start,
                End = entity.End,
                Location = entity.Location ?? string.Empty,
                RelatedClass = entity.RelatedClass ?? string.Empty,
                Desc = entity.Desc ?? string.Empty
            });
        }

        [HttpDelete("events/{eventId:int}")]
        public async Task<IActionResult> DeleteEvent([FromRoute] int eventId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            var entity = await _context.CalendarEvents
                .FirstOrDefaultAsync(x => x.Id == eventId && x.UserId == userId);

            if (entity == null)
                return NotFound(new { message = "Etkinlik bulunamadı." });

            _context.CalendarEvents.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Etkinlik silindi." });
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrWhiteSpace(userIdStr) && int.TryParse(userIdStr, out userId);
        }

        private static bool TryValidateEventDto(UpsertCalendarEventDto? dto, out DateTime parsedDate, out string message)
        {
            parsedDate = default;
            message = string.Empty;

            if (dto == null)
            {
                message = "Geçersiz istek gövdesi.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                message = "Başlık zorunlu.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.Type))
            {
                message = "Kategori zorunlu.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.Date) || !DateTime.TryParse(dto.Date, out parsedDate))
            {
                message = "Tarih geçersiz.";
                return false;
            }

            if (!IsValidHourMinute(dto.Start) || !IsValidHourMinute(dto.End))
            {
                message = "Saat formatı HH:mm olmalıdır.";
                return false;
            }

            return true;
        }

        private static bool IsValidHourMinute(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return TimeOnly.TryParse(value.Trim(), out _);
        }

        private static string NormalizeColor(string? raw)
        {
            var text = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return "#2e86c1";

            if (!text.StartsWith("#", StringComparison.Ordinal))
                text = "#" + text;

            return text;
        }

        private static string NormalizeTextColor(string? raw, string backgroundColor)
        {
            var text = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return GetContrastTextColor(backgroundColor);

            if (!text.StartsWith("#", StringComparison.Ordinal))
                text = "#" + text;

            return text;
        }

        private static string GetContrastTextColor(string? backgroundColor)
        {
            var hex = (backgroundColor ?? string.Empty).Trim();
            if (hex.StartsWith("#", StringComparison.Ordinal))
                hex = hex.Substring(1);

            if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
                return "#ffffff";

            var r = (rgb >> 16) & 0xFF;
            var g = (rgb >> 8) & 0xFF;
            var b = rgb & 0xFF;
            var luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            return luminance > 158 ? "#111111" : "#ffffff";
        }

        private static string MakeTypeKey(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "kategori";

            string key = input.Trim().ToLowerInvariant();
            key = key.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u").Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
            key = new string(key.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray());
            key = string.Join("-", key.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(key) ? "kategori" : key;
        }
    }
}
