using ExperimentSimulation.DataAccessLayer.Concrete;
using ExperimentSimulation.EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace ExperimentSimulation.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClassController : ControllerBase
    {
        private readonly Context _context;
        private static readonly ConcurrentDictionary<string, HashSet<int>> ActivityLikes = new();
        private static readonly ConcurrentDictionary<string, List<ActivityCommentDto>> ActivityComments = new();
        private static readonly object ActivityLock = new();

        public ClassController(Context context)
        {
            _context = context;
        }

        public class MyClassDto
        {
            public int Id { get; set; }
            public string Code { get; set; } = null!;
            public string? Name { get; set; }
            public string? TeacherName { get; set; }

            public string? GradeLevel { get; set; }
            public string? LessonName { get; set; }

            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? JoinedAt { get; set; }
            public int StudentCount { get; set; }
            public int AssignmentCount { get; set; }
            public int SuccessRatePercent { get; set; }
            public string Status { get; set; } = UserClass.StatusApproved;
        }

        public class JoinRequestDto
        {
            public int UserId { get; set; }
            public string Name { get; set; } = null!;
            public string Surname { get; set; } = null!;
            public string Email { get; set; } = null!;
            public DateTime RequestedAt { get; set; }
            public string Status { get; set; } = null!;
        }

        public class ClassStudentDto
        {
            public int UserId { get; set; }
            public string Name { get; set; } = null!;
            public string Surname { get; set; } = null!;
            public string Email { get; set; } = null!;
            public DateTime? JoinedAt { get; set; }

            public int SuccessRatePercent { get; set; }
        }

        public class StudentProfileHistoryItemDto
        {
            public string Title { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
            public DateTime Date { get; set; }
        }

        public class StudentProfileDto
        {
            public int StudentId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Surname { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime? LastLogin { get; set; }
            public DateTime? JoinedAt { get; set; }
            public int PerformancePercent { get; set; }
            public int CompletedAssignments { get; set; }
            public int TotalAssignments { get; set; }
            public int CompletedExperiments { get; set; }
            public string ParticipationLevel { get; set; } = "Düşük";
            public int CurrentStreakDays { get; set; }
            public List<StudentProfileHistoryItemDto> AssignmentHistory { get; set; } = new();
            public List<StudentProfileHistoryItemDto> ExperimentHistory { get; set; } = new();
        }

        public class ClassActivityDto
        {
            public string ActivityId { get; set; } = null!;
            public string Type { get; set; } = null!;
            public string Title { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string? ActorName { get; set; }
            public int? ActorUserId { get; set; }
            public string? ActorRole { get; set; }
            public DateTime OccurredAt { get; set; }
            public int LikesCount { get; set; }
            public bool IsLikedByCurrentUser { get; set; }
            public List<ActivityCommentDto> Comments { get; set; } = new();
        }

        public class ActivityCommentDto
        {
            public int UserId { get; set; }
            public string UserName { get; set; } = null!;
            public string UserRole { get; set; } = "Teacher";
            public string Text { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
        }

        public class CreateActivityCommentDto
        {
            public string Text { get; set; } = null!;
        }

        public class CreateClassDto
        {
            public string Name { get; set; } = null!;
            public string? GradeLevel { get; set; }
            public string? LessonName { get; set; }
        }

        public class JoinClassDto
        {
            public string ClassCode { get; set; } = null!;
        }

        public class UpdateClassStatusDto
        {
            public bool IsActive { get; set; }
        }

        // GET /api/Class/my
        [HttpGet("my")]
        public async Task<IActionResult> My()
        {
            await DeactivateExpiredAssignmentsAsync();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            const string ROLE_STUDENT = "Student";
            const string ROLE_TEACHER = "Teacher";
            const string STATUS_APPROVED = UserClass.StatusApproved;

            bool isAdmin = User.IsInRole("Admin");

            if (isAdmin)
            {
                var all = await _context.Classes
                    .AsNoTracking()
                    .Select(c => new MyClassDto
                    {
                        Id = c.Id,
                        Code = c.Code,
                        Name = c.Name,

                        TeacherName = _context.UserClasses
                            .Where(x =>
                                x.ClassId == c.Id &&
                                x.MemberRole == ROLE_TEACHER &&
                                x.Status == STATUS_APPROVED)
                            .Select(x => x.User.Name + " " + x.User.Surname)
                            .FirstOrDefault(),

                        GradeLevel = c.GradeLevel,
                        LessonName = c.LessonName,

                        IsActive = c.IsActive,
                        CreatedAt = c.CreatedAt,
                        JoinedAt = null,
                        Status = UserClass.StatusApproved,

                        StudentCount = _context.UserClasses
                            .Count(uc =>
                                uc.ClassId == c.Id &&
                                uc.MemberRole == ROLE_STUDENT &&
                                uc.Status == STATUS_APPROVED),

                        AssignmentCount = _context.Assignments
    .Count(a => a.ClassId == c.Id),

                        // Bunu sonra foreach ile gerçek değere çekeceğiz.
                        SuccessRatePercent = 0
                    })
                    .ToListAsync();

                foreach (var cls in all)
                {
                    cls.SuccessRatePercent = await CalculateClassSuccessRateFromStudentsAsync(cls.Id);
                }

                return Ok(all);
            }

            var my = await _context.UserClasses
                .AsNoTracking()
                .Where(uc =>
                    uc.UserId == userId &&
                    (
                        uc.MemberRole == "Teacher" ||
                        (
                            uc.MemberRole == "Student" &&
                            (
                                uc.Status == UserClass.StatusApproved ||
                                uc.Status == UserClass.StatusPending
                            )
                        )
                    ))
                .Select(uc => new MyClassDto
                {
                    Id = uc.Class.Id,
                    Code = uc.Class.Code,
                    Name = uc.Class.Name,

                    TeacherName = _context.UserClasses
                        .Where(x =>
                            x.ClassId == uc.ClassId &&
                            x.MemberRole == ROLE_TEACHER &&
                            x.Status == STATUS_APPROVED)
                        .Select(x => x.User.Name + " " + x.User.Surname)
                        .FirstOrDefault(),

                    GradeLevel = uc.Class.GradeLevel,
                    LessonName = uc.Class.LessonName,

                    IsActive = uc.Class.IsActive,
                    CreatedAt = uc.Class.CreatedAt,
                    JoinedAt = uc.JoinedAt,
                    Status = uc.Status,

                    StudentCount = _context.UserClasses
                        .Count(x =>
                            x.ClassId == uc.ClassId &&
                            x.MemberRole == ROLE_STUDENT &&
                            x.Status == STATUS_APPROVED),

                    AssignmentCount = _context.Assignments
    .Count(a => a.ClassId == uc.ClassId),

                    // Bunu sonra foreach ile gerçek değere çekeceğiz.
                    SuccessRatePercent = 0
                })
                .ToListAsync();

            foreach (var cls in my)
            {
                cls.SuccessRatePercent = await CalculateClassSuccessRateFromStudentsAsync(cls.Id);
            }

            return Ok(my);
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

        // POST /api/Class
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateClassDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Sınıf adı zorunlu." });

            var classEntity = new Class
            {
                Name = dto.Name.Trim(),
                GradeLevel = string.IsNullOrWhiteSpace(dto.GradeLevel) ? null : dto.GradeLevel.Trim(),
                LessonName = string.IsNullOrWhiteSpace(dto.LessonName) ? null : dto.LessonName.Trim(),
                Code = await GenerateUniqueCodeAsync(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Classes.AddAsync(classEntity);
            await _context.SaveChangesAsync();

            var link = new UserClass
            {
                UserId = userId,
                ClassId = classEntity.Id,
                MemberRole = "Teacher",
                Status = UserClass.StatusApproved,
                RequestedAt = DateTime.UtcNow,
                JoinedAt = DateTime.UtcNow
            };

            await _context.UserClasses.AddAsync(link);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                classEntity.Id,
                classEntity.Code,
                classEntity.Name,
                classEntity.GradeLevel,
                classEntity.LessonName,
                classEntity.IsActive,
                classEntity.CreatedAt
            });
        }

        // POST /api/Class/join
        [HttpPost("join")]
        public async Task<IActionResult> Join([FromBody] JoinClassDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (dto == null || string.IsNullOrWhiteSpace(dto.ClassCode))
                return BadRequest(new { message = "Sınıf kodu zorunlu." });

            string code = dto.ClassCode.Trim().ToUpperInvariant();

            var cls = await _context.Classes
                .FirstOrDefaultAsync(c => c.Code == code);

            if (cls == null)
                return NotFound(new { message = "Sınıf bulunamadı." });

            if (!cls.IsActive)
                return BadRequest(new { message = "Bu sınıf pasif." });

            var membership = await _context.UserClasses
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ClassId == cls.Id);

            if (membership != null)
            {
                if (membership.Status == UserClass.StatusApproved)
                {
                    return Ok(new
                    {
                        message = "Zaten bu sınıftasın.",
                        classId = cls.Id,
                        classCode = cls.Code,
                        className = cls.Name,
                        status = membership.Status
                    });
                }

                if (membership.Status == UserClass.StatusPending)
                {
                    return Ok(new
                    {
                        message = "Katılma isteğin zaten beklemede.",
                        classId = cls.Id,
                        classCode = cls.Code,
                        className = cls.Name,
                        status = membership.Status
                    });
                }

                membership.Status = UserClass.StatusPending;
                membership.MemberRole = "Student";
                membership.JoinedAt = null;
                membership.RequestedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Katılma isteğin tekrar gönderildi ve onay bekliyor.",
                    classId = cls.Id,
                    classCode = cls.Code,
                    className = cls.Name,
                    status = membership.Status
                });
            }

            await _context.UserClasses.AddAsync(new UserClass
            {
                UserId = userId,
                ClassId = cls.Id,
                MemberRole = "Student",
                Status = UserClass.StatusPending,
                RequestedAt = DateTime.UtcNow,
                JoinedAt = null
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Katılma isteğin gönderildi. Öğretmen onayı bekleniyor.",
                classId = cls.Id,
                classCode = cls.Code,
                className = cls.Name,
                status = UserClass.StatusPending
            });
        }

        // POST /api/Class/{classId}/status
        [HttpPost("{classId:int}/status")]
        public async Task<IActionResult> UpdateClassStatus(int classId, [FromBody] UpdateClassStatusDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (!await IsTeacherOrAdminOfClass(userId, classId))
                return Forbid();

            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId);
            if (cls == null)
                return NotFound(new { message = "Sınıf bulunamadı." });

            cls.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                classId = cls.Id,
                isActive = cls.IsActive,
                message = cls.IsActive ? "Sınıf aktif duruma alındı." : "Sınıf pasif duruma alındı."
            });
        }

        [HttpGet("{classId:int}/join-requests")]
        public async Task<IActionResult> GetJoinRequests(int classId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            bool isAdmin = User.IsInRole("Admin");

            bool isTeacherOfClass = await _context.UserClasses
                .AsNoTracking()
                .AnyAsync(uc =>
                    uc.UserId == userId &&
                    uc.ClassId == classId &&
                    uc.MemberRole == "Teacher" &&
                    uc.Status == UserClass.StatusApproved);

            if (!isAdmin && !isTeacherOfClass)
                return Forbid();

            var requests = await _context.UserClasses
                .AsNoTracking()
                .Include(uc => uc.User)
                .Where(uc => uc.ClassId == classId && uc.Status == UserClass.StatusPending && uc.MemberRole == "Student")
                .OrderByDescending(uc => uc.RequestedAt)
                .Select(uc => new JoinRequestDto
                {
                    UserId = uc.UserId,
                    Name = uc.User.Name,
                    Surname = uc.User.Surname,
                    Email = uc.User.Email,
                    RequestedAt = uc.RequestedAt,
                    Status = uc.Status
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("{classId:int}/students")]
        public async Task<IActionResult> GetClassStudents(int classId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            bool isAdmin = User.IsInRole("Admin");
            bool isTeacherOfClass = await _context.UserClasses
                .AsNoTracking()
                .AnyAsync(uc =>
                    uc.UserId == userId &&
                    uc.ClassId == classId &&
                    uc.MemberRole == "Teacher" &&
                    uc.Status == UserClass.StatusApproved);

            if (!isAdmin && !isTeacherOfClass)
                return Forbid();

            var students = await _context.UserClasses
    .AsNoTracking()
    .Include(uc => uc.User)
    .Where(uc =>
        uc.ClassId == classId &&
        uc.MemberRole == "Student" &&
        uc.Status == UserClass.StatusApproved)
    .OrderBy(uc => uc.User.Name)
    .ThenBy(uc => uc.User.Surname)
    .Select(uc => new ClassStudentDto
    {
        UserId = uc.UserId,
        Name = uc.User.Name,
        Surname = uc.User.Surname,
        Email = uc.User.Email,
        JoinedAt = uc.JoinedAt,
        SuccessRatePercent = 0
    })
    .ToListAsync();

            foreach (var student in students)
            {
                student.SuccessRatePercent = await CalculateStudentSuccessRateAsync(classId, student.UserId);
            }

            return Ok(students);
        }

        [HttpGet("{classId:int}/students/{studentId:int}/profile")]
        public async Task<IActionResult> GetStudentProfile(int classId, int studentId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            bool isAdmin = User.IsInRole("Admin");

            bool isTeacherOfClass = await _context.UserClasses
                .AsNoTracking()
                .AnyAsync(uc =>
                    uc.UserId == userId &&
                    uc.ClassId == classId &&
                    uc.MemberRole == "Teacher" &&
                    uc.Status == UserClass.StatusApproved);

            if (!isAdmin && !isTeacherOfClass)
                return Forbid();

            var classEntity = await _context.Classes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (classEntity == null)
                return NotFound(new { message = "Sınıf bulunamadı." });

            var membership = await _context.UserClasses
                .AsNoTracking()
                .Where(uc =>
                    uc.ClassId == classId &&
                    uc.UserId == studentId &&
                    uc.MemberRole == "Student" &&
                    uc.Status == UserClass.StatusApproved)
                .Select(uc => new { uc.JoinedAt })
                .FirstOrDefaultAsync();

            if (membership == null)
                return NotFound(new { message = "Öğrenci sınıfta bulunamadı." });

            var student = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == studentId);

            if (student == null)
                return NotFound(new { message = "Öğrenci bulunamadı." });

            int totalAssignments = await _context.Assignments
                .AsNoTracking()
                .CountAsync(a => a.ClassId == classId);

            var results = await _context.AssignmentResults
                .AsNoTracking()
                .Include(r => r.Assignment)
                    .ThenInclude(a => a.Experiment)
                .Where(r =>
                    r.StudentId == studentId &&
                    r.Assignment.ClassId == classId &&
                    r.IsCompleted)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();

            int completedAssignments = results
                .Select(r => r.AssignmentId)
                .Distinct()
                .Count();

            int completedExperiments = results
                .Where(r => r.Assignment != null)
                .Select(r => r.Assignment.ExperimentId)
                .Distinct()
                .Count();

            int performance = results.Count > 0
                ? (int)Math.Round(results.Average(r => r.Score))
                : 0;

            var assignmentHistory = results
                .Select(r => new StudentProfileHistoryItemDto
                {
                    Title = r.Assignment != null && !string.IsNullOrWhiteSpace(r.Assignment.Title)
                        ? r.Assignment.Title
                        : "Ödev",

                    Value = $"{r.CorrectCount} doğru / {r.WrongCount} yanlış • %{r.Score}",

                    Date = r.CompletedAt
                })
                .ToList();

            var experimentHistory = results
                .Select(r => new StudentProfileHistoryItemDto
                {
                    Title = r.Assignment != null &&
                            r.Assignment.Experiment != null &&
                            !string.IsNullOrWhiteSpace(r.Assignment.Experiment.ExperimentName)
                        ? r.Assignment.Experiment.ExperimentName
                        : "Deney",

                    Value = $"{r.CorrectCount} doğru / {r.WrongCount} yanlış • %{r.Score}",

                    Date = r.CompletedAt
                })
                .ToList();

            var sessions = await _context.UserSessionActivities
                .AsNoTracking()
                .Where(s => s.UserId == studentId)
                .OrderBy(s => s.LoginAt)
                .ToListAsync();

            var activeDates = new HashSet<DateTime>();

            foreach (var s in sessions)
            {
                var end = s.LogoutAt ?? s.LastSeenAt;

                if (end < s.LoginAt)
                    end = s.LoginAt;

                for (var d = s.LoginAt.Date; d <= end.Date; d = d.AddDays(1))
                    activeDates.Add(d);
            }

            int currentStreakDays = 0;
            var cursor = DateTime.UtcNow.Date;

            while (activeDates.Contains(cursor))
            {
                currentStreakDays++;
                cursor = cursor.AddDays(-1);
            }

            string participation = activeDates.Count >= 20
                ? "Yüksek"
                : activeDates.Count >= 8
                    ? "Orta"
                    : "Düşük";

            var dto = new StudentProfileDto
            {
                StudentId = student.Id,
                Name = student.Name,
                Surname = student.Surname,
                Email = student.Email,
                CreatedAt = student.CreatedAt,
                LastLogin = student.LastLogin,
                JoinedAt = membership.JoinedAt,

                PerformancePercent = performance,
                CompletedAssignments = completedAssignments,
                TotalAssignments = totalAssignments,
                CompletedExperiments = completedExperiments,

                ParticipationLevel = participation,
                CurrentStreakDays = currentStreakDays,

                AssignmentHistory = assignmentHistory,
                ExperimentHistory = experimentHistory
            };

            return Ok(dto);
        }

        [HttpGet("{classId:int}/activity")]
        public async Task<IActionResult> GetClassActivity(int classId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (!await IsTeacherOfClass(userId, classId))
                return Forbid();

            var activity = await BuildClassActivityItems(classId, userId, studentScope: false);
            return Ok(activity);
        }

        [HttpGet("{classId:int}/activity/student")]
        public async Task<IActionResult> GetClassActivityForStudent(int classId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            bool isApprovedStudentOfClass = await _context.UserClasses
                .AsNoTracking()
                .AnyAsync(uc =>
                    uc.UserId == userId &&
                    uc.ClassId == classId &&
                    uc.MemberRole == "Student" &&
                    uc.Status == UserClass.StatusApproved);

            if (!isApprovedStudentOfClass && !await IsTeacherOrAdminOfClass(userId, classId))
                return Forbid();

            var activity = await BuildClassActivityItems(classId, userId, studentScope: true);
            return Ok(activity);
        }

        [HttpGet("activity/personal")]
        public async Task<IActionResult> GetPersonalActivity()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            var currentUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var personal = new List<ClassActivityDto>();
            if (currentUser != null)
            {
                string fullName = $"{currentUser.Name} {currentUser.Surname}".Trim();
                personal.Add(new ClassActivityDto
                {
                    ActivityId = $"account-created-{userId}-{currentUser.CreatedAt.Ticks}",
                    Type = "AccountCreated",
                    Title = "Hesap Oluşturuldu",
                    Description = "Hesabın başarıyla oluşturuldu.",
                    ActorName = string.IsNullOrWhiteSpace(fullName) ? "Kullanıcı" : fullName,
                    ActorUserId = userId,
                    ActorRole = currentUser.RoleId == 2 ? "Teacher"
                        : currentUser.RoleId == 1 ? "Student"
                        : currentUser.RoleId == 3 ? "Admin"
                        : "User",
                    OccurredAt = currentUser.CreatedAt
                });

                var assignedTasks = await _context.ContentTasks
                    .AsNoTracking()
                    .Include(t => t.AssigneeUser)
                    .Where(t => t.CreatedByUserId == userId)
                    .OrderByDescending(t => t.CreatedAtUtc)
                    .Take(100)
                    .ToListAsync();

                foreach (var task in assignedTasks)
                {
                    if (task == null)
                        continue;

                    string taskTitle = string.IsNullOrWhiteSpace(task.Title) ? "Görev" : task.Title.Trim();
                    string assigneeName = task.AssigneeUser != null
                        ? $"{task.AssigneeUser.Name} {task.AssigneeUser.Surname}".Trim()
                        : "İçerik üreticisi";

                    personal.Add(new ClassActivityDto
                    {
                        ActivityId = $"content-task-assigned-{task.Id}-{task.CreatedAtUtc.Ticks}",
                        Type = "TaskAssigned",
                        Title = "Görev Atandı",
                        Description = $"\"{taskTitle}\" görevi {assigneeName} isimli içerik üreticisine atandı.",
                        ActorName = string.IsNullOrWhiteSpace(fullName) ? "Kullanıcı" : fullName,
                        ActorUserId = userId,
                        ActorRole = currentUser.RoleId == 2 ? "Teacher"
                            : currentUser.RoleId == 1 ? "Student"
                            : currentUser.RoleId == 3 ? "Admin"
                            : "User",
                        OccurredAt = task.CreatedAtUtc
                    });
                }
            }

            var classIds = await _context.UserClasses
                .AsNoTracking()
                .Where(uc => uc.UserId == userId && uc.Status == UserClass.StatusApproved)
                .Select(uc => uc.ClassId)
                .Distinct()
                .ToListAsync();

            if (classIds.Count == 0)
                return Ok(personal.OrderByDescending(x => x.OccurredAt).ToList());

            var all = new List<ClassActivityDto>();
            foreach (var classId in classIds)
            {
                var classItems = await BuildClassActivityItems(classId, userId, studentScope: false);
                foreach (var item in classItems)
                {
                    if (item != null && item.ActorUserId.HasValue && item.ActorUserId.Value == userId)
                        all.Add(item);
                }
            }

            all.AddRange(personal);

            return Ok(all
                .OrderByDescending(x => x.OccurredAt)
                .ToList());
        }

        [HttpPost("{classId:int}/students/{studentId:int}/remove")]
        public async Task<IActionResult> RemoveStudentFromClass(int classId, int studentId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (!await IsTeacherOfClass(userId, classId))
                return Forbid();

            var membership = await _context.UserClasses
                .FirstOrDefaultAsync(uc =>
                    uc.ClassId == classId &&
                    uc.UserId == studentId &&
                    uc.MemberRole == "Student" &&
                    uc.Status == UserClass.StatusApproved);

            if (membership == null)
                return NotFound(new { message = "Öğrenci bu sınıfta bulunamadı." });

            _context.UserClasses.Remove(membership);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Öğrenci sınıftan çıkarıldı." });
        }

        [HttpPost("{classId:int}/activity/{activityId}/like")]
        public async Task<IActionResult> LikeActivity(int classId, string activityId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (!await IsTeacherOfClass(userId, classId))
                return Forbid();

            if (!await ActivityExists(classId, activityId, userId))
                return NotFound(new { message = "Aktivite bulunamadı." });

            lock (ActivityLock)
            {
                if (!ActivityLikes.TryGetValue(activityId, out var likes))
                {
                    likes = new HashSet<int>();
                    ActivityLikes[activityId] = likes;
                }

                likes.Add(userId);
            }

            return Ok(new { message = "Beğeni kaydedildi." });
        }

        [HttpPost("{classId:int}/activity/{activityId}/unlike")]
        public async Task<IActionResult> UnlikeActivity(int classId, string activityId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (!await IsTeacherOrAdminOfClass(userId, classId))
                return Forbid();

            if (!await ActivityExists(classId, activityId, userId))
                return NotFound(new { message = "Aktivite bulunamadı." });

            lock (ActivityLock)
            {
                if (ActivityLikes.TryGetValue(activityId, out var likes))
                    likes.Remove(userId);
            }

            return Ok(new { message = "Beğeni kaldırıldı." });
        }

        [HttpPost("{classId:int}/activity/{activityId}/comments")]
        public async Task<IActionResult> AddActivityComment(int classId, string activityId, [FromBody] CreateActivityCommentDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            if (!await IsTeacherOrAdminOfClass(userId, classId))
                return Forbid();

            if (dto == null || string.IsNullOrWhiteSpace(dto.Text))
                return BadRequest(new { message = "Yorum metni zorunlu." });

            if (!await ActivityExists(classId, activityId, userId))
                return NotFound(new { message = "Aktivite bulunamadı." });

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            var comment = new ActivityCommentDto
            {
                UserId = userId,
                UserName = $"{user.Name} {user.Surname}".Trim(),
                UserRole = "Teacher",
                Text = dto.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            lock (ActivityLock)
            {
                if (!ActivityComments.TryGetValue(activityId, out var comments))
                {
                    comments = new List<ActivityCommentDto>();
                    ActivityComments[activityId] = comments;
                }

                comments.Add(comment);
            }

            return Ok(comment);
        }

        [HttpPost("{classId:int}/join-requests/{studentId:int}/approve")]
        public async Task<IActionResult> ApproveJoinRequest(int classId, int studentId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            bool isAdmin = User.IsInRole("Admin");
            bool isTeacherOfClass = await _context.UserClasses
                .AsNoTracking()
                .AnyAsync(uc =>
                    uc.UserId == userId &&
                    uc.ClassId == classId &&
                    uc.MemberRole == "Teacher" &&
                    uc.Status == UserClass.StatusApproved);

            if (!isAdmin && !isTeacherOfClass)
                return Forbid();

            var membership = await _context.UserClasses
                .FirstOrDefaultAsync(uc =>
                    uc.ClassId == classId &&
                    uc.UserId == studentId &&
                    uc.MemberRole == "Student");

            if (membership == null)
                return NotFound(new { message = "Katılma isteği bulunamadı." });

            if (membership.Status == UserClass.StatusApproved)
                return Ok(new { message = "Öğrenci zaten onaylı üye." });

            membership.Status = UserClass.StatusApproved;
            membership.JoinedAt = DateTime.UtcNow;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (user != null && user.RoleId != 1)
            {
                user.RoleId = 1;
                user.Role = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Katılma isteği onaylandı." });
        }

        [HttpPost("{classId:int}/join-requests/{studentId:int}/reject")]
        public async Task<IActionResult> RejectJoinRequest(int classId, int studentId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            bool isAdmin = User.IsInRole("Admin");
            bool isTeacherOfClass = await _context.UserClasses
                .AsNoTracking()
                .AnyAsync(uc =>
                    uc.UserId == userId &&
                    uc.ClassId == classId &&
                    uc.MemberRole == "Teacher" &&
                    uc.Status == UserClass.StatusApproved);

            if (!isAdmin && !isTeacherOfClass)
                return Forbid();

            var membership = await _context.UserClasses
                .FirstOrDefaultAsync(uc =>
                    uc.ClassId == classId &&
                    uc.UserId == studentId &&
                    uc.MemberRole == "Student" &&
                    uc.Status == UserClass.StatusPending);

            if (membership == null)
                return NotFound(new { message = "Bekleyen katılma isteği bulunamadı." });

            membership.Status = UserClass.StatusRejected;
            membership.JoinedAt = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Katılma isteği reddedildi." });
        }

        private static string GenerateClassCode()
        {
            return Guid.NewGuid().ToString("N")[..6].ToUpper();
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            while (true)
            {
                var code = GenerateClassCode();
                bool exists = await _context.Classes.AnyAsync(c => c.Code == code);
                if (!exists) return code;
            }
        }

        private async Task<bool> IsTeacherOrAdminOfClass(int userId, int classId)
        {
            if (User.IsInRole("Admin"))
                return true;

            return await IsTeacherOfClass(userId, classId);
        }

        private async Task<bool> IsTeacherOfClass(int userId, int classId)
        {
            return await _context.UserClasses
                .AsNoTracking()
                .AnyAsync(uc =>
                    uc.UserId == userId &&
                    uc.ClassId == classId &&
                    uc.MemberRole == "Teacher" &&
                    uc.Status == UserClass.StatusApproved);
        }

        private async Task<bool> ActivityExists(int classId, string activityId, int userId)
        {
            if (string.IsNullOrWhiteSpace(activityId))
                return false;

            var all = await BuildClassActivityItems(classId, userId, studentScope: false);
            return all.Any(a => a.ActivityId == activityId);
        }

        private async Task<int> CalculateStudentSuccessRateAsync(int classId, int studentId)
        {
            var scores = await _context.AssignmentResults
                .AsNoTracking()
                .Where(r =>
                    r.StudentId == studentId &&
                    r.IsCompleted &&
                    r.Assignment.ClassId == classId)
                .Select(r => r.Score)
                .ToListAsync();

            if (scores.Count == 0)
                return 0;

            return (int)Math.Round(scores.Average());
        }

        private async Task<int> CalculateClassSuccessRateFromStudentsAsync(int classId)
        {
            var studentIds = await _context.UserClasses
                .AsNoTracking()
                .Where(uc =>
                    uc.ClassId == classId &&
                    uc.MemberRole == "Student" &&
                    uc.Status == UserClass.StatusApproved)
                .Select(uc => uc.UserId)
                .ToListAsync();

            if (studentIds.Count == 0)
                return 0;

            int totalRate = 0;

            foreach (var studentId in studentIds)
            {
                int studentRate = await CalculateStudentSuccessRateAsync(classId, studentId);
                totalRate += studentRate;
            }

            return (int)Math.Round(totalRate / (double)studentIds.Count);
        }

        private async Task<List<ClassActivityDto>> BuildClassActivityItems(int classId, int currentUserId, bool studentScope)
        {
            var cls = await _context.Classes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (cls == null)
                return new List<ClassActivityDto>();

            var teacher = await _context.UserClasses
                .AsNoTracking()
                .Include(uc => uc.User)
                .Where(uc =>
                    uc.ClassId == classId &&
                    uc.MemberRole == "Teacher" &&
                    uc.Status == UserClass.StatusApproved)
                .OrderBy(uc => uc.JoinedAt ?? uc.RequestedAt)
                .FirstOrDefaultAsync();

            string teacherName = teacher != null
                ? $"{teacher.User.Name} {teacher.User.Surname}".Trim()
                : "Öğretmen";

            var activity = new List<ClassActivityDto>();
            var classCreatedId = $"class-created-{classId}-{cls.CreatedAt.Ticks}";
            string className = string.IsNullOrWhiteSpace(cls.Name) ? "Sınıf" : cls.Name;
            activity.Add(new ClassActivityDto
            {
                ActivityId = classCreatedId,
                Type = "ClassCreated",
                Title = "Sınıf Oluşturuldu",
                Description = $"\"{className}\" sınıfı oluşturuldu.",
                ActorName = teacherName,
                ActorUserId = teacher?.UserId,
                ActorRole = "Teacher",
                OccurredAt = cls.CreatedAt
            });

            var approvals = await _context.UserClasses
                .AsNoTracking()
                .Include(uc => uc.User)
                .Where(uc =>
                    uc.ClassId == classId &&
                    uc.MemberRole == "Student" &&
                    uc.Status == UserClass.StatusApproved &&
                    uc.JoinedAt != null)
                .ToListAsync();

            foreach (var member in approvals)
            {
                string fullName = $"{member.User.Name} {member.User.Surname}".Trim();
                var joinedAt = member.JoinedAt!.Value;
                activity.Add(new ClassActivityDto
                {
                    ActivityId = $"join-approved-{classId}-{member.UserId}-{joinedAt.Ticks}",
                    Type = "JoinApproved",
                    Title = "Katılım Onaylandı",
                    Description = string.IsNullOrWhiteSpace(fullName)
                        ? $"Bir öğrencinin \"{className}\" sınıfına katılımı onaylandı."
                        : $"{fullName}, \"{className}\" sınıfına katıldı.",
                    ActorName = fullName,
                    ActorUserId = member.UserId,
                    ActorRole = "Student",
                    OccurredAt = joinedAt
                });
            }

            var assignments = await _context.Assignments
                .AsNoTracking()
                .Where(a => a.ClassId == classId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                string assignmentTitle = string.IsNullOrWhiteSpace(assignment.Title) ? "Ödev" : assignment.Title;
                activity.Add(new ClassActivityDto
                {
                    ActivityId = $"assignment-created-{classId}-{assignment.Id}-{assignment.CreatedAt.Ticks}",
                    Type = "AssignmentCreated",
                    Title = "Ödev Eklendi",
                    Description = $"{assignmentTitle} ödevi eklendi.",
                    ActorName = teacherName,
                    ActorUserId = teacher?.UserId,
                    ActorRole = "Teacher",
                    OccurredAt = assignment.CreatedAt
                });
            }

            if (studentScope)
            {
                activity = activity
                    .Where(a =>
                        string.Equals(a.ActorRole, "Teacher", StringComparison.OrdinalIgnoreCase) ||
                        (a.ActorUserId.HasValue && a.ActorUserId.Value == currentUserId))
                    .ToList();
            }

            foreach (var item in activity)
            {
                lock (ActivityLock)
                {
                    if (ActivityLikes.TryGetValue(item.ActivityId, out var likes))
                    {
                        item.LikesCount = likes.Count;
                        item.IsLikedByCurrentUser = likes.Contains(currentUserId);
                    }

                    if (ActivityComments.TryGetValue(item.ActivityId, out var comments))
                    {
                        item.Comments = comments
                            .OrderBy(c => c.CreatedAt)
                            .Select(c => new ActivityCommentDto
                            {
                                UserId = c.UserId,
                                UserName = c.UserName,
                                UserRole = c.UserRole,
                                Text = c.Text,
                                CreatedAt = c.CreatedAt
                            })
                            .ToList();
                    }
                }
            }

            return activity
                .OrderByDescending(x => x.OccurredAt)
                .ToList();
        }
    }
}