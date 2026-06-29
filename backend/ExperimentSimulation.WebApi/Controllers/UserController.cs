using ExperimentSimulation.BusinessLayer.Abstract;
using ExperimentSimulation.DataAccessLayer.Concrete;
using ExperimentSimulation.EntityLayer.Concrete;
using ExperimentSimulation.WebApi.Dtos;
using ExperimentSimulation.WebApi.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ExperimentSimulation.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private const int TeacherRoleId = 2;
        private const int IndependentRoleId = 3;

        private readonly IUserService _userService;
        private readonly Context _context;

        public UserController(IUserService userService, Context context)
        {
            _userService = userService;
            _context = context;
        }

        public class CreateTeacherRoleRequestDto
        {
            public string? Note { get; set; }
        }

        public class ReviewTeacherRoleRequestDto
        {
            public string? Note { get; set; }
        }

        private int? TryGetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out int userId))
                return null;

            return userId;
        }

        [HttpGet]
        public IActionResult UserList()
        {
            var values = _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Select(u => new {
                    u.Id,
                    u.Name,
                    u.Surname,
                    u.Email,
                    u.RoleId,
                    RoleName = u.Role != null ? u.Role.Name : "",
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLogin
                })
                .ToList();

            return Ok(values);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> AddUser(CreateUserDto dto)
        {
            bool exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists) return BadRequest("Bu email zaten kayıtlı.");

            // BirthDate parse
            DateTime? birthDate = null;
            if (!string.IsNullOrWhiteSpace(dto.BirthDate))
            {
                if (DateTime.TryParse(dto.BirthDate, out var bd)) birthDate = bd;
                else return BadRequest("BirthDate formatı geçersiz.");
            }

            // Class codes normalize
            var codes = (dto.ClassCodes ?? new List<string>())
                .Select(x => (x ?? "").Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            const int ROLE_INDEPENDENT = 3;

            // ClassCodes doluysa sınıfları bul
            List<Class> classesToJoin = new();
            bool invalidClassCodeEntered = false;

            if (codes.Count > 0)
            {
                classesToJoin = await _context.Classes
                    .Where(c => codes.Contains(c.Code) && c.IsActive)
                    .ToListAsync();

                // Kod girildi ama eşleşen aktif sınıf bulunamadıysa
                // kayıt yine devam etsin, kullanıcı bağımsız olsun
                if (classesToJoin.Count == 0)
                    invalidClassCodeEntered = true;
            }

            int finalRoleId = ROLE_INDEPENDENT;

            var (hashB64, saltB64) = PasswordHasher.HashPassword(dto.Password);

            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new User
                {
                    Name = dto.Name.Trim(),
                    Surname = dto.Surname.Trim(),
                    Email = dto.Email.Trim(),

                    PasswordHash = hashB64,
                    PasswordSalt = saltB64,

                    RoleId = finalRoleId,
                    IsActive = true,

                    Phone = dto.Phone,
                    BirthDate = birthDate,

                    CreatedAt = DateTime.UtcNow,
                    Role = null
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Sınıf kodu girildiyse doğrudan üyelik yerine bekleyen istek oluştur
                if (classesToJoin.Count > 0)
                {
                    foreach (var cls in classesToJoin)
                    {
                        bool alreadyMember = await _context.UserClasses
                            .AnyAsync(uc => uc.UserId == user.Id && uc.ClassId == cls.Id);

                        if (!alreadyMember)
                        {
                            _context.UserClasses.Add(new UserClass
                            {
                                UserId = user.Id,
                                ClassId = cls.Id,
                                MemberRole = "Student",
                                Status = UserClass.StatusPending,
                                RequestedAt = DateTime.UtcNow,
                                JoinedAt = null
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();

                return Ok(new
                {
                    message = classesToJoin.Count > 0
        ? "User created and join request sent."
        : invalidClassCodeEntered
            ? "User created as independent because class code was invalid."
            : "User created as independent.",
                    userId = user.Id,
                    roleId = user.RoleId,
                    pendingRequestCount = classesToJoin.Count,
                    classCodeAccepted = classesToJoin.Count > 0,
                    fellBackToIndependent = invalidClassCodeEntered
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Kayıt sırasında hata oluştu.", detail = ex.Message });
            }
        }

        [Authorize(Roles = "Admin,Yönetici")]
        [HttpDelete]
        public IActionResult DeleteUser(int id)
        {
            var values = _userService.TGetByID(id);
            _userService.TDelete(values);
            return Ok();
        }

        [HttpPut]
        public IActionResult UpdateUser(User user)
        {
            _userService.TUpdate(user);
            return Ok();
        }

        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            var values = _userService.TGetByID(id);
            return Ok(values);
        }

        public class AssignRoleDto
        {
            public int RoleId { get; set; }
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> AssignRole(int id, [FromBody] AssignRoleDto dto)
        {
            if (dto == null || dto.RoleId <= 0)
                return BadRequest(new { message = "RoleId zorunlu." });

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            user.RoleId = dto.RoleId;
            user.Role = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rol atandı.", user.Id, user.RoleId });
        }

        [HttpPost("teacher-role-request")]
        public async Task<IActionResult> SubmitTeacherRoleRequest([FromBody] CreateTeacherRoleRequestDto? dto)
        {
            var userId = TryGetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Token içinde user id yok." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            if (user.RoleId != IndependentRoleId)
                return BadRequest(new { message = "Öğretmen başvurusu yalnızca bağımsız kullanıcı için geçerlidir." });

            var existingPending = await _context.TeacherRoleRequests
                .AsNoTracking()
                .Where(r => r.UserId == userId.Value && r.Status == TeacherRoleRequest.StatusPending)
                .OrderByDescending(r => r.RequestedAtUtc)
                .FirstOrDefaultAsync();

            if (existingPending != null)
            {
                return Ok(new
                {
                    existingPending.Id,
                    existingPending.Status,
                    existingPending.RequestedAtUtc,
                    message = "Öğretmen başvurun zaten beklemede."
                });
            }

            var request = new TeacherRoleRequest
            {
                UserId = userId.Value,
                Status = TeacherRoleRequest.StatusPending,
                RequestedAtUtc = DateTime.UtcNow,
                Note = string.IsNullOrWhiteSpace(dto?.Note) ? null : dto.Note.Trim()
            };

            _context.TeacherRoleRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                request.Id,
                request.Status,
                request.RequestedAtUtc,
                message = "Öğretmen başvurun gönderildi."
            });
        }

        [HttpGet("teacher-role-request/me")]
        public async Task<IActionResult> GetMyTeacherRoleRequestState()
        {
            var userId = TryGetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Token içinde user id yok." });

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            var latest = await _context.TeacherRoleRequests
                .AsNoTracking()
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.RequestedAtUtc)
                .FirstOrDefaultAsync();

            if (latest == null)
            {
                return Ok(new
                {
                    HasRequest = false,
                    Status = user.RoleId == TeacherRoleId ? TeacherRoleRequest.StatusApproved : "None",
                    RequestedAtUtc = (DateTime?)null,
                    ReviewedAtUtc = (DateTime?)null,
                    DecisionNote = "",
                    Note = ""
                });
            }

            return Ok(new
            {
                HasRequest = true,
                latest.Id,
                latest.Status,
                latest.Note,
                latest.DecisionNote,
                latest.RequestedAtUtc,
                latest.ReviewedAtUtc,
                latest.ReviewedByUserId
            });
        }

        [Authorize(Roles = "Admin,Yönetici")]
        [HttpGet("teacher-role-requests")]
        public async Task<IActionResult> GetTeacherRoleRequests([FromQuery] string? status = null)
        {
            var query = _context.TeacherRoleRequests
                .AsNoTracking()
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
                query = query.Where(r => r.Status == status);

            var items = await query
                .OrderByDescending(r => r.RequestedAtUtc)
                .Select(r => new
                {
                    r.Id,
                    r.UserId,
                    Name = r.User.Name,
                    Surname = r.User.Surname,
                    Email = r.User.Email,
                    r.Status,
                    r.Note,
                    r.DecisionNote,
                    r.RequestedAtUtc,
                    r.ReviewedAtUtc,
                    r.ReviewedByUserId
                })
                .ToListAsync();

            return Ok(items);
        }

        [Authorize(Roles = "Admin,Yönetici")]
        [HttpPost("teacher-role-requests/{requestId:int}/approve")]
        public async Task<IActionResult> ApproveTeacherRoleRequest(int requestId, [FromBody] ReviewTeacherRoleRequestDto? dto)
        {
            var adminId = TryGetCurrentUserId();
            if (!adminId.HasValue)
                return Unauthorized(new { message = "Token içinde user id yok." });

            var request = await _context.TeacherRoleRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return NotFound(new { message = "Başvuru bulunamadı." });

            if (!string.Equals(request.Status, TeacherRoleRequest.StatusPending, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Sadece bekleyen başvuru onaylanabilir." });

            request.Status = TeacherRoleRequest.StatusApproved;
            request.ReviewedAtUtc = DateTime.UtcNow;
            request.ReviewedByUserId = adminId.Value;
            request.DecisionNote = string.IsNullOrWhiteSpace(dto?.Note) ? null : dto.Note.Trim();

            if (request.User != null)
            {
                request.User.RoleId = TeacherRoleId;
                request.User.Role = null;
            }

            var otherPending = await _context.TeacherRoleRequests
                .Where(r => r.UserId == request.UserId && r.Id != request.Id && r.Status == TeacherRoleRequest.StatusPending)
                .ToListAsync();

            foreach (var pending in otherPending)
            {
                pending.Status = TeacherRoleRequest.StatusRejected;
                pending.ReviewedAtUtc = DateTime.UtcNow;
                pending.ReviewedByUserId = adminId.Value;
                pending.DecisionNote = "Başka bir öğretmen başvurusu onaylandı.";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                request.Id,
                request.UserId,
                request.Status,
                message = "Öğretmen başvurusu onaylandı."
            });
        }

        [Authorize(Roles = "Admin,Yönetici")]
        [HttpPost("teacher-role-requests/{requestId:int}/reject")]
        public async Task<IActionResult> RejectTeacherRoleRequest(int requestId, [FromBody] ReviewTeacherRoleRequestDto? dto)
        {
            var adminId = TryGetCurrentUserId();
            if (!adminId.HasValue)
                return Unauthorized(new { message = "Token içinde user id yok." });

            var request = await _context.TeacherRoleRequests
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return NotFound(new { message = "Başvuru bulunamadı." });

            if (!string.Equals(request.Status, TeacherRoleRequest.StatusPending, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Sadece bekleyen başvuru reddedilebilir." });

            request.Status = TeacherRoleRequest.StatusRejected;
            request.ReviewedAtUtc = DateTime.UtcNow;
            request.ReviewedByUserId = adminId.Value;
            request.DecisionNote = string.IsNullOrWhiteSpace(dto?.Note) ? null : dto.Note.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                request.Id,
                request.UserId,
                request.Status,
                message = "Öğretmen başvurusu reddedildi."
            });
        }

        [HttpGet("me")]
        public IActionResult GetMe()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(idClaim))
                return Unauthorized(new { message = "Token içinde id claim yok." });

            if (!int.TryParse(idClaim, out var userId))
                return Unauthorized(new { message = "Token id claim geçersiz." });

            var user = _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            var sessions = _context.UserSessionActivities
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.LoginAt)
                .ToList();

            double totalHours = 0;
            var activeDates = new HashSet<DateTime>();

            foreach (var s in sessions)
            {
                var end = s.LogoutAt ?? s.LastSeenAt;
                if (end < s.LoginAt)
                    end = s.LoginAt;

                totalHours += (end - s.LoginAt).TotalHours;

                var startDate = s.LoginAt.Date;
                var endDate = end.Date;
                for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    activeDates.Add(d);
            }

            int currentStreakDays = 0;
            var cursor = DateTime.UtcNow.Date;
            while (activeDates.Contains(cursor))
            {
                currentStreakDays++;
                cursor = cursor.AddDays(-1);
            }

            return Ok(new
            {
                id = user.Id,
                name = user.Name,
                surname = user.Surname,
                email = user.Email,
                createdAt = user.CreatedAt,
                lastLogin = user.LastLogin,
                isActive = user.IsActive,
                totalActiveDays = activeDates.Count,
                totalActiveHours = Math.Round(totalHours, 1),
                currentActiveStreakDays = currentStreakDays,
                roleName = user.Role != null ? user.Role.Name : ""
            });
        }

        [HttpGet("session/weekly-hours")]
        public IActionResult GetWeeklySessionHours()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            var nowLocal = DateTime.Now;
            int mondayOffset = ((int)nowLocal.DayOfWeek + 6) % 7;
            var weekStartLocal = nowLocal.Date.AddDays(-mondayOffset);
            var weekEndLocal = weekStartLocal.AddDays(7);

            var weekStartUtc = weekStartLocal.ToUniversalTime();
            var weekEndUtc = weekEndLocal.ToUniversalTime();

            var sessions = _context.UserSessionActivities
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.LoginAt < weekEndUtc && (s.LogoutAt ?? s.LastSeenAt) > weekStartUtc)
                .ToList();

            var dailyHours = new double[7];

            foreach (var s in sessions)
            {
                var endUtc = s.LogoutAt ?? s.LastSeenAt;
                if (endUtc < s.LoginAt)
                    endUtc = s.LoginAt;

                var startLocal = DateTime.SpecifyKind(s.LoginAt, DateTimeKind.Utc).ToLocalTime();
                var endLocal = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc).ToLocalTime();

                var clippedStart = startLocal < weekStartLocal ? weekStartLocal : startLocal;
                var clippedEnd = endLocal > weekEndLocal ? weekEndLocal : endLocal;

                if (clippedEnd <= clippedStart)
                    continue;

                var cursor = clippedStart;
                while (cursor < clippedEnd)
                {
                    var dayEnd = cursor.Date.AddDays(1);
                    var chunkEnd = clippedEnd < dayEnd ? clippedEnd : dayEnd;
                    int dayIndex = (int)(cursor.Date - weekStartLocal).TotalDays;

                    if (dayIndex >= 0 && dayIndex < 7)
                        dailyHours[dayIndex] += (chunkEnd - cursor).TotalHours;

                    cursor = chunkEnd;
                }
            }

            string[] dayLabels = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };

            var items = Enumerable.Range(0, 7)
                .Select(i => new
                {
                    dayIndex = i,
                    dayLabel = dayLabels[i],
                    hours = Math.Round(dailyHours[i], 1)
                })
                .ToList();

            return Ok(new
            {
                weekStart = weekStartLocal,
                items
            });
        }

        [HttpPost("session/heartbeat")]
        public async Task<IActionResult> Heartbeat()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            var current = await _context.UserSessionActivities
                .Where(s => s.UserId == userId && s.LogoutAt == null)
                .OrderByDescending(s => s.LoginAt)
                .FirstOrDefaultAsync();

            if (current == null)
                return Ok(new { updated = false });

            current.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { updated = true });
        }

        [HttpPost("session/end")]
        public async Task<IActionResult> EndSession()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("id")?.Value;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out int userId))
                return Unauthorized(new { message = "Token içinde user id yok." });

            var current = await _context.UserSessionActivities
                .Where(s => s.UserId == userId && s.LogoutAt == null)
                .OrderByDescending(s => s.LoginAt)
                .FirstOrDefaultAsync();

            if (current == null)
                return Ok(new { closed = false });

            var now = DateTime.UtcNow;
            current.LogoutAt = current.LastSeenAt > current.LoginAt ? current.LastSeenAt : now;
            await _context.SaveChangesAsync();
            return Ok(new { closed = true });
        }
    }
}