using ExperimentSimulation.DataAccessLayer.Concrete;
using ExperimentSimulation.EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace ExperimentSimulation.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentTaskController : ControllerBase
    {
        private readonly Context _context;

        private const string StatusAssigned = "Atandı";
        private const string StatusInReview = "İncelemede";
        private const string StatusInRevision = "Revizyonda";
        private const string StatusCompleted = "Tamamlandı";

        public ContentTaskController(Context context)
        {
            _context = context;
        }

        public class CreateContentTaskDto
        {
            public string title { get; set; } = "";
            public string taskType { get; set; } = "";
            public string experimentName { get; set; } = "";
            public string estimatedDuration { get; set; } = "";
            public string startDate { get; set; } = "";
            public string deadline { get; set; } = "";
            public int assigneeUserId { get; set; }
            public string priority { get; set; } = "Orta";
            public string description { get; set; } = "";
            public string expectedOutput { get; set; } = "";
        }

        public class ContentTaskItemDto
        {
            public int id { get; set; }
            public string title { get; set; } = "";
            public string taskType { get; set; } = "";
            public string experimentName { get; set; } = "";
            public string estimatedDuration { get; set; } = "";
            public string startDate { get; set; } = "";
            public string deadline { get; set; } = "";
            public int assigneeUserId { get; set; }
            public string assigneeName { get; set; } = "";
            public string priority { get; set; } = "Orta";
            public string status { get; set; } = "Atandı";
            public int progressPercent { get; set; }
            public string description { get; set; } = "";
            public string expectedOutput { get; set; } = "";
            public int createdByUserId { get; set; }
            public string createdByName { get; set; } = "";
            public string createdAtUtc { get; set; } = "";
            public string updatedAtUtc { get; set; } = "";
            public string latestRevisionType { get; set; } = "";
            public string latestRevisionPriority { get; set; } = "";
            public string latestRevisionDeadline { get; set; } = "";
            public string latestRevisionNote { get; set; } = "";
            public string latestRevisionRequestedAt { get; set; } = "";
        }

        public class ContentCreatorLiteDto
        {
            public int userId { get; set; }
            public string fullName { get; set; } = "";
            public string email { get; set; } = "";
        }

        public class ContentTaskSummaryDto
        {
            public int activeCount { get; set; }
            public int todayDeadlineCount { get; set; }
            public int overdueCount { get; set; }
            public int reviewCount { get; set; }
        }

        public class TransitionTaskStatusDto
        {
            public string revisionType { get; set; } = "İçerik";
            public string priority { get; set; } = "Orta";
            public string deadline { get; set; } = "";
            public string note { get; set; } = "";
        }

        public class ContentTaskCommentDto
        {
            public int userId { get; set; }
            public string userName { get; set; } = "";
            public string text { get; set; } = "";
            public string createdAt { get; set; } = "";
        }

        public class CreateContentTaskCommentDto
        {
            public string text { get; set; } = "";
        }

        [HttpGet("creators")]
        [Authorize(Roles = "Admin,Yönetici")]
        public async Task<IActionResult> GetCreators()
        {
            var creators = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.IsActive &&
                    (u.RoleId == 4 ||
                     (u.Role != null &&
                      (u.Role.Name == "ContentCreator" ||
                       u.Role.Name == "İçerik Üreticisi" ||
                       u.Role.Name == "Icerik Ureticisi"))))
                .OrderBy(u => u.Name)
                .ThenBy(u => u.Surname)
                .Select(u => new ContentCreatorLiteDto
                {
                    userId = u.Id,
                    fullName = ((u.Name ?? "") + " " + (u.Surname ?? "")).Trim(),
                    email = u.Email ?? ""
                })
                .ToListAsync();

            return Ok(creators);
        }

        [HttpGet("summary")]
        [Authorize(Roles = "Admin,Yönetici")]
        public async Task<IActionResult> GetSummary()
        {
            var today = DateTime.Today;
            var tasks = _context.ContentTasks.AsNoTracking();

            int active = await tasks.CountAsync();
            int todayDeadline = await tasks.CountAsync(t => t.Deadline.Date == today);
            int overdue = await tasks.CountAsync(t => t.Deadline.Date < today && !string.Equals(t.Status, StatusCompleted));
            var allStatuses = await tasks
                .Select(t => t.Status)
                .ToListAsync();

            int review = allStatuses.Count(s =>
            {
                string normalized = NormalizeStatusForMatch(s ?? "");
                return normalized.Contains("incele") || normalized.Contains("review");
            });

            return Ok(new ContentTaskSummaryDto
            {
                activeCount = active,
                todayDeadlineCount = todayDeadline,
                overdueCount = overdue,
                reviewCount = review
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Yönetici")]
        public async Task<IActionResult> GetAll()
        {
            var all = await _context.ContentTasks
                .AsNoTracking()
                .Include(t => t.AssigneeUser)
                .Include(t => t.CreatedByUser)
                .OrderByDescending(t => t.UpdatedAtUtc)
                .Select(t => ToDto(
                    t,
                    t.AssigneeUser != null ? ((t.AssigneeUser.Name ?? "") + " " + (t.AssigneeUser.Surname ?? "")).Trim() : "-",
                    t.CreatedByUser != null ? ((t.CreatedByUser.Name ?? "") + " " + (t.CreatedByUser.Surname ?? "")).Trim() : "-"))
                .ToListAsync();

            return Ok(all);
        }

        [HttpGet("{taskId:int}")]
        [Authorize(Roles = "Admin,Yönetici,ContentCreator,İçerik Üreticisi,Icerik Ureticisi")]
        public async Task<IActionResult> GetById(int taskId)
        {
            var task = await _context.ContentTasks
                .AsNoTracking()
                .Include(t => t.AssigneeUser)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            int userId = GetCurrentUserId();
            if (!UserCanAccessTask(task, userId))
                return Forbid();

            string assigneeName = task.AssigneeUser != null ? ((task.AssigneeUser.Name ?? "") + " " + (task.AssigneeUser.Surname ?? "")).Trim() : "-";
            string createdByName = task.CreatedByUser != null ? ((task.CreatedByUser.Name ?? "") + " " + (task.CreatedByUser.Surname ?? "")).Trim() : "-";
            var dto = ToDto(task, assigneeName, createdByName);
            await PopulateLatestRevisionData(dto, taskId);
            return Ok(dto);
        }

        [HttpGet("{taskId:int}/comments")]
        [Authorize(Roles = "Admin,Yönetici,ContentCreator,İçerik Üreticisi,Icerik Ureticisi")]
        public async Task<IActionResult> GetComments(int taskId)
        {
            var task = await _context.ContentTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            int userId = GetCurrentUserId();
            if (!UserCanAccessTask(task, userId))
                return Forbid();

            var comments = await _context.ContentTaskComments
                .AsNoTracking()
                .Include(c => c.User)
                .Where(c => c.ContentTaskId == taskId)
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new ContentTaskCommentDto
                {
                    userId = c.UserId,
                    userName = ((c.User.Name ?? "") + " " + (c.User.Surname ?? "")).Trim(),
                    text = c.Text,
                    createdAt = c.CreatedAtUtc.ToString("O")
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpPost("{taskId:int}/comments")]
        [Authorize(Roles = "Admin,Yönetici,ContentCreator,İçerik Üreticisi,Icerik Ureticisi")]
        public async Task<IActionResult> AddComment(int taskId, [FromBody] CreateContentTaskCommentDto dto)
        {
            var task = await _context.ContentTasks
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            int userId = GetCurrentUserId();
            if (!UserCanAccessTask(task, userId))
                return Forbid();

            string text = (dto?.text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest(new { message = "Yorum metni zorunlu." });

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            string fullName = user != null ? ((user.Name ?? "") + " " + (user.Surname ?? "")).Trim() : "Kullanıcı";
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = "Kullanıcı";

            var entity = new ContentTaskComment
            {
                ContentTaskId = taskId,
                UserId = userId,
                Text = text,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.ContentTaskComments.Add(entity);
            task.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var comment = new ContentTaskCommentDto
            {
                userId = userId,
                userName = fullName,
                text = text,
                createdAt = entity.CreatedAtUtc.ToString("O")
            };

            return Ok(comment);
        }

        [HttpGet("my")]
        [Authorize(Roles = "ContentCreator,İçerik Üreticisi,Icerik Ureticisi")]
        public async Task<IActionResult> GetMine()
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(new { message = "Kullanıcı doğrulanamadı." });

            var mine = await _context.ContentTasks
                .AsNoTracking()
                .Include(t => t.AssigneeUser)
                .Include(t => t.CreatedByUser)
                .Where(t => t.AssigneeUserId == userId)
                .OrderByDescending(t => t.UpdatedAtUtc)
                .Select(t => ToDto(
                    t,
                    t.AssigneeUser != null ? ((t.AssigneeUser.Name ?? "") + " " + (t.AssigneeUser.Surname ?? "")).Trim() : "-",
                    t.CreatedByUser != null ? ((t.CreatedByUser.Name ?? "") + " " + (t.CreatedByUser.Surname ?? "")).Trim() : "-"))
                .ToListAsync();

            return Ok(mine);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Yönetici")]
        public async Task<IActionResult> Create([FromBody] CreateContentTaskDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Geçersiz istek." });

            if (string.IsNullOrWhiteSpace(dto.title))
                return BadRequest(new { message = "Görev başlığı zorunlu." });

            if (dto.assigneeUserId <= 0)
                return BadRequest(new { message = "Atanacak içerik üreticisi zorunlu." });

            if (!TryParseDate(dto.startDate, out var startDate))
                return BadRequest(new { message = "Başlangıç tarihi geçersiz." });

            if (!TryParseDate(dto.deadline, out var deadline))
                return BadRequest(new { message = "Son teslim tarihi geçersiz." });

            var assignee = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == dto.assigneeUserId);

            if (assignee == null)
                return NotFound(new { message = "Atanacak kullanıcı bulunamadı." });

            bool isCreator = assignee.RoleId == 4 ||
                (assignee.Role != null &&
                 (assignee.Role.Name == "ContentCreator" ||
                  assignee.Role.Name == "İçerik Üreticisi" ||
                  assignee.Role.Name == "Icerik Ureticisi"));

            if (!isCreator)
                return BadRequest(new { message = "Seçilen kullanıcı içerik üreticisi rolünde değil." });

            int adminId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var entity = new ContentTask
            {
                Title = dto.title.Trim(),
                TaskType = (dto.taskType ?? "").Trim(),
                ExperimentName = (dto.experimentName ?? "").Trim(),
                EstimatedDuration = (dto.estimatedDuration ?? "").Trim(),
                StartDate = startDate,
                Deadline = deadline,
                AssigneeUserId = dto.assigneeUserId,
                Priority = string.IsNullOrWhiteSpace(dto.priority) ? "Orta" : dto.priority.Trim(),
                Status = StatusAssigned,
                ProgressPercent = 0,
                Description = (dto.description ?? "").Trim(),
                ExpectedOutput = (dto.expectedOutput ?? "").Trim(),
                CreatedByUserId = adminId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _context.ContentTasks.Add(entity);
            await _context.SaveChangesAsync();

            string assigneeName = ((assignee.Name ?? "") + " " + (assignee.Surname ?? "")).Trim();
            string createdByName = "-";
            var creator = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == adminId);
            if (creator != null)
                createdByName = ((creator.Name ?? "") + " " + (creator.Surname ?? "")).Trim();

            return Ok(ToDto(entity, assigneeName, createdByName));
        }

        [HttpPost("{taskId:int}/submit")]
        [Authorize(Roles = "ContentCreator,İçerik Üreticisi,Icerik Ureticisi")]
        public async Task<IActionResult> SubmitForReview(int taskId, [FromBody] TransitionTaskStatusDto? dto)
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(new { message = "Kullanıcı doğrulanamadı." });

            var task = await _context.ContentTasks
                .Include(t => t.AssigneeUser)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            if (task.AssigneeUserId != userId)
                return Forbid();

            if (string.Equals(task.Status?.Trim(), StatusInReview, StringComparison.OrdinalIgnoreCase))
            {
                string alreadyAssigneeName = task.AssigneeUser != null ? ((task.AssigneeUser.Name ?? "") + " " + (task.AssigneeUser.Surname ?? "")).Trim() : "-";
                string alreadyCreatedByName = task.CreatedByUser != null ? ((task.CreatedByUser.Name ?? "") + " " + (task.CreatedByUser.Surname ?? "")).Trim() : "-";
                return Ok(ToDto(task, alreadyAssigneeName, alreadyCreatedByName));
            }

            if (!CanSubmitForReview(task.Status))
                return BadRequest(new { message = $"Bu görev '{task.Status}' durumundan incelemeye gönderilemez." });

            task.Status = StatusInReview;
            task.ProgressPercent = Math.Max(task.ProgressPercent, 90);
            task.UpdatedAtUtc = DateTime.UtcNow;

            int affected = await _context.SaveChangesAsync();
            if (affected <= 0)
                return StatusCode(500, new { message = "Görev durumu veritabanına kaydedilemedi." });

            var persisted = await _context.ContentTasks
                .AsNoTracking()
                .Include(t => t.AssigneeUser)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (persisted == null)
                return StatusCode(500, new { message = "Görev kaydı doğrulanamadı." });

            string assigneeName = persisted.AssigneeUser != null ? ((persisted.AssigneeUser.Name ?? "") + " " + (persisted.AssigneeUser.Surname ?? "")).Trim() : "-";
            string createdByName = persisted.CreatedByUser != null ? ((persisted.CreatedByUser.Name ?? "") + " " + (persisted.CreatedByUser.Surname ?? "")).Trim() : "-";
            return Ok(ToDto(persisted, assigneeName, createdByName));
        }

        [HttpPost("{taskId:int}/request-revision")]
        [Authorize(Roles = "Admin,Yönetici")]
        public async Task<IActionResult> RequestRevision(int taskId, [FromBody] TransitionTaskStatusDto? dto)
        {
            var task = await _context.ContentTasks
                .Include(t => t.AssigneeUser)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            if (!string.Equals(task.Status, StatusInReview, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = $"Revizyon yalnızca '{StatusInReview}' durumundaki görev için istenebilir." });

            int adminId = GetCurrentUserId();
            if (adminId <= 0)
                return Unauthorized(new { message = "Kullanıcı doğrulanamadı." });

            string revisionType = string.IsNullOrWhiteSpace(dto?.revisionType) ? "İçerik" : dto!.revisionType.Trim();
            string priority = string.IsNullOrWhiteSpace(dto?.priority) ? "Orta" : dto!.priority.Trim();
            string note = (dto?.note ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(note))
                return BadRequest(new { message = "Revizyon notu zorunlu." });

            DateTime? newDeadline = null;
            if (!string.IsNullOrWhiteSpace(dto?.deadline))
            {
                if (!TryParseDate(dto!.deadline, out var parsedDeadline))
                    return BadRequest(new { message = "Revizyon son teslim tarihi geçersiz." });

                newDeadline = parsedDeadline;
            }

            task.Status = StatusInRevision;
            task.Priority = priority;
            if (newDeadline.HasValue)
                task.Deadline = newDeadline.Value;
            task.UpdatedAtUtc = DateTime.UtcNow;

            _context.ContentTaskRevisionRequests.Add(new ContentTaskRevisionRequest
            {
                ContentTaskId = task.Id,
                RequestedByUserId = adminId,
                RevisionType = revisionType,
                Priority = priority,
                NewDeadline = newDeadline,
                Note = note,
                IsResolved = false,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            string assigneeName = task.AssigneeUser != null ? ((task.AssigneeUser.Name ?? "") + " " + (task.AssigneeUser.Surname ?? "")).Trim() : "-";
            string createdByName = task.CreatedByUser != null ? ((task.CreatedByUser.Name ?? "") + " " + (task.CreatedByUser.Surname ?? "")).Trim() : "-";
            var response = ToDto(task, assigneeName, createdByName);
            await PopulateLatestRevisionData(response, task.Id);
            return Ok(response);
        }

        private async Task PopulateLatestRevisionData(ContentTaskItemDto dto, int taskId)
        {
            var latest = await _context.ContentTaskRevisionRequests
                .AsNoTracking()
                .Where(r => r.ContentTaskId == taskId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (latest == null)
                return;

            dto.latestRevisionType = latest.RevisionType ?? string.Empty;
            dto.latestRevisionPriority = latest.Priority ?? string.Empty;
            dto.latestRevisionDeadline = latest.NewDeadline.HasValue ? latest.NewDeadline.Value.ToString("yyyy-MM-dd") : string.Empty;
            dto.latestRevisionNote = latest.Note ?? string.Empty;
            dto.latestRevisionRequestedAt = latest.CreatedAtUtc.ToString("O");
        }

        [HttpPost("{taskId:int}/approve")]
        [Authorize(Roles = "Admin,Yönetici")]
        public async Task<IActionResult> ApproveTask(int taskId, [FromBody] TransitionTaskStatusDto? dto)
        {
            var task = await _context.ContentTasks
                .Include(t => t.AssigneeUser)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound(new { message = "Görev bulunamadı." });

            if (!string.Equals(task.Status, StatusInReview, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = $"Onay yalnızca '{StatusInReview}' durumundaki görev için verilebilir." });

            task.Status = StatusCompleted;
            task.ProgressPercent = 100;
            task.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            string assigneeName = task.AssigneeUser != null ? ((task.AssigneeUser.Name ?? "") + " " + (task.AssigneeUser.Surname ?? "")).Trim() : "-";
            string createdByName = task.CreatedByUser != null ? ((task.CreatedByUser.Name ?? "") + " " + (task.CreatedByUser.Surname ?? "")).Trim() : "-";
            return Ok(ToDto(task, assigneeName, createdByName));
        }

        private static ContentTaskItemDto ToDto(ContentTask entity, string assigneeName, string createdByName)
        {
            return new ContentTaskItemDto
            {
                id = entity.Id,
                title = entity.Title,
                taskType = entity.TaskType,
                experimentName = entity.ExperimentName,
                estimatedDuration = entity.EstimatedDuration,
                startDate = entity.StartDate.ToString("yyyy-MM-dd"),
                deadline = entity.Deadline.ToString("yyyy-MM-dd"),
                assigneeUserId = entity.AssigneeUserId,
                assigneeName = string.IsNullOrWhiteSpace(assigneeName) ? "-" : assigneeName,
                priority = entity.Priority,
                status = entity.Status,
                progressPercent = entity.ProgressPercent,
                description = entity.Description,
                expectedOutput = entity.ExpectedOutput,
                createdByUserId = entity.CreatedByUserId,
                createdByName = string.IsNullOrWhiteSpace(createdByName) ? "-" : createdByName,
                createdAtUtc = entity.CreatedAtUtc.ToString("O"),
                updatedAtUtc = entity.UpdatedAtUtc.ToString("O")
            };
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            if (int.TryParse(idClaim, out int id))
                return id;
            return 0;
        }

        private bool UserCanAccessTask(ContentTask task, int currentUserId)
        {
            if (task == null)
                return false;

            if (User.IsInRole("Admin") || User.IsInRole("Yönetici"))
                return true;

            return currentUserId > 0 && task.AssigneeUserId == currentUserId;
        }

        private static bool CanSubmitForReview(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            string normalized = NormalizeStatusForMatch(status);

            return normalized == "atandi"
                || normalized.Contains("atand")
                || normalized == "revizyonda"
                || normalized.Contains("revizyon");
        }

        private static string NormalizeStatusForMatch(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            string input = raw.Trim().ToLowerInvariant().Replace('ı', 'i');
            string decomposed = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (char c in decomposed)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool TryParseDate(string raw, out DateTime dt)
        {
            dt = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            if (DateTime.TryParse(raw, out var parsed))
            {
                dt = parsed.Date;
                return true;
            }

            return false;
        }
    }
}
