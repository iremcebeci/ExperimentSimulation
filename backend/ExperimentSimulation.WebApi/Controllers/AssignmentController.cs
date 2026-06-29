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
    [Authorize]
    public class AssignmentController : ControllerBase
    {
        private readonly Context _context;

        public AssignmentController(Context context)
        {
            _context = context;
        }

        public class CreateAssignmentDto
        {
            public string Title { get; set; } = null!;
            public int ClassId { get; set; }
            public DateTime StartDate { get; set; }
            public int DurationDays { get; set; }
            public int ExperimentId { get; set; }
        }

        public class AssignmentDto
        {
            public int Id { get; set; }
            public string Title { get; set; } = null!;
            public int ClassId { get; set; }
            public string ClassName { get; set; } = null!;
            public bool IsActive { get; set; }
            public int ExperimentId { get; set; }
            public string ExperimentName { get; set; } = null!;
            public DateTime StartDate { get; set; }
            public int DurationDays { get; set; }
            public DateTime CreatedAt { get; set; }

            public int CorrectCount { get; set; }
            public int WrongCount { get; set; }
            public int TotalQuestionCount { get; set; }
            public int Score { get; set; }
            public bool IsCompleted { get; set; }

            public string? SceneName { get; set; }
            public string? ExperimentKey { get; set; }

            public int CompletedStudentCount { get; set; }
            public int IncompleteStudentCount { get; set; }
            public int ClassStudentCount { get; set; }
            public int CompletionPercent { get; set; }
        }

        // GET /api/Assignment/my
        [HttpGet("my")]
        public async Task<IActionResult> My()
        {
            await DeactivateExpiredAssignmentsAsync();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            var myClassIds = await _context.UserClasses
                .AsNoTracking()
                .Where(uc => uc.UserId == userId && uc.Status == UserClass.StatusApproved)
                .Select(uc => uc.ClassId)
                .Distinct()
                .ToListAsync();

            var items = await _context.Assignments
                .AsNoTracking()
                .Include(a => a.Class)
                .Include(a => a.Experiment)
                .Where(a => myClassIds.Contains(a.ClassId))
                .OrderBy(a => a.StartDate)
                .Select(a => new AssignmentDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    ClassId = a.ClassId,
                    ClassName = a.Class != null ? (a.Class.Name ?? "-") : "-",
                    IsActive = a.IsActive,
                    ExperimentId = a.ExperimentId,
                    ExperimentName = a.Experiment != null ? (a.Experiment.ExperimentName ?? "-") : "-",
                    StartDate = a.StartDate,
                    DurationDays = a.DurationDays,
                    CreatedAt = a.CreatedAt,

                    SceneName = a.Experiment != null ? a.Experiment.SceneName : null,
                    ExperimentKey = a.Experiment != null ? a.Experiment.ExperimentKey : null,

                    CorrectCount = _context.AssignmentResults
                        .Where(r => r.AssignmentId == a.Id && r.StudentId == userId)
                        .Select(r => r.CorrectCount)
                        .FirstOrDefault(),

                    WrongCount = _context.AssignmentResults
                        .Where(r => r.AssignmentId == a.Id && r.StudentId == userId)
                        .Select(r => r.WrongCount)
                        .FirstOrDefault(),

                    TotalQuestionCount = _context.AssignmentResults
                        .Where(r => r.AssignmentId == a.Id && r.StudentId == userId)
                        .Select(r => r.TotalQuestionCount)
                        .FirstOrDefault(),

                    Score = _context.AssignmentResults
                        .Where(r => r.AssignmentId == a.Id && r.StudentId == userId)
                        .Select(r => r.Score)
                        .FirstOrDefault(),

                    IsCompleted = _context.AssignmentResults
                        .Any(r => r.AssignmentId == a.Id && r.StudentId == userId && r.IsCompleted),

                    ClassStudentCount = _context.UserClasses
    .Where(uc =>
        uc.ClassId == a.ClassId &&
        uc.Status == UserClass.StatusApproved &&
        uc.MemberRole == "Student")
    .Count(),

                    CompletedStudentCount = _context.AssignmentResults
    .Where(r => r.AssignmentId == a.Id && r.IsCompleted)
    .Count(),

                    IncompleteStudentCount =
    _context.UserClasses
        .Where(uc =>
            uc.ClassId == a.ClassId &&
            uc.Status == UserClass.StatusApproved &&
            uc.MemberRole == "Student")
        .Count()
    -
    _context.AssignmentResults
        .Where(r => r.AssignmentId == a.Id && r.IsCompleted)
        .Count(),

                    CompletionPercent =
    _context.UserClasses
        .Where(uc =>
            uc.ClassId == a.ClassId &&
            uc.Status == UserClass.StatusApproved &&
            uc.MemberRole == "Student")
        .Count() <= 0
        ? 0
        : (int)Math.Round(
            _context.AssignmentResults
                .Where(r => r.AssignmentId == a.Id && r.IsCompleted)
                .Count()
            /
            (double)_context.UserClasses
                .Where(uc =>
                    uc.ClassId == a.ClassId &&
                    uc.Status == UserClass.StatusApproved &&
                    uc.MemberRole == "Student")
                .Count()
            * 100
        )
                })
                .ToListAsync();

            return Ok(items);
        }

        private async Task DeactivateExpiredAssignmentsAsync()
        {
            var today = DateTime.UtcNow.Date;

            var activeItems = await _context.Assignments
                .Where(a => a.IsActive)
                .ToListAsync();

            bool hasChanges = false;

            foreach (var item in activeItems)
            {
                if (item == null)
                    continue;

                int duration = item.DurationDays <= 0 ? 1 : item.DurationDays;
                var endExclusive = item.StartDate.Date.AddDays(duration);

                if (today >= endExclusive)
                {
                    item.IsActive = false;
                    hasChanges = true;
                }
            }

            if (hasChanges)
                await _context.SaveChangesAsync();
        }

        // POST /api/Assignment
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAssignmentDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { message = "Ödev başlığı zorunlu." });

            if (dto.ClassId <= 0)
                return BadRequest(new { message = "ClassId zorunlu." });

            if (dto.DurationDays <= 0)
                return BadRequest(new { message = "Süre en az 1 gün olmalı." });

            if (dto.ExperimentId <= 0)
                return BadRequest(new { message = "ExperimentId zorunlu." });

            var teacherOwnsClass = await _context.UserClasses
                .AnyAsync(uc => uc.UserId == userId &&
                                uc.ClassId == dto.ClassId &&
                                uc.MemberRole == "Teacher" &&
                                uc.Status == UserClass.StatusApproved);

            if (!teacherOwnsClass)
                return Forbid();

            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == dto.ClassId);

            if (cls == null)
                return NotFound(new { message = "Sınıf bulunamadı." });

            var experiment = await _context.Experiments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == dto.ExperimentId && e.IsActive);

            if (experiment == null)
                return NotFound(new { message = "Deney bulunamadı." });

            var entity = new Assignment
            {
                Title = dto.Title.Trim(),
                ClassId = dto.ClassId,
                ExperimentId = dto.ExperimentId,
                StartDate = dto.StartDate,
                DurationDays = dto.DurationDays,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Assignments.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new AssignmentDto
            {
                Id = entity.Id,
                Title = entity.Title,
                ClassId = entity.ClassId,
                ClassName = cls.Name ?? "-",
                IsActive = entity.IsActive,
                ExperimentId = entity.ExperimentId,
                ExperimentName = experiment.ExperimentName ?? "-",
                StartDate = entity.StartDate,
                DurationDays = entity.DurationDays,
                CreatedAt = entity.CreatedAt,

                SceneName = experiment.SceneName,
                ExperimentKey = experiment.ExperimentKey,

                CorrectCount = 0,
                WrongCount = 0,
                TotalQuestionCount = 0,
                Score = 0,
                IsCompleted = false
            });
        }
    }
}