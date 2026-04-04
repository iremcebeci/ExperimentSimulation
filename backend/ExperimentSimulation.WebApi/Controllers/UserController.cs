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
        private readonly IUserService _userService;
        private readonly Context _context;

        public UserController(IUserService userService, Context context)
        {
            _userService = userService;
            _context = context;
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