using ExperimentSimulation.DataAccessLayer.Concrete;
using ExperimentSimulation.EntityLayer.Concrete;
using ExperimentSimulation.WebApi.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ExperimentSimulation.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Context _context;

        public AuthController(Context context)
        {
            _context = context;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = null!;
            public string Password { get; set; } = null!;
        }

        public class LoginResponse
        {
            public string Token { get; set; } = null!;
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public string Surname { get; set; } = null!;
            public string Email { get; set; } = null!;
            public int RoleId { get; set; }
            public string RoleName { get; set; } = null!;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Email ve şifre zorunlu." });

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user == null)
                return Unauthorized(new { message = "Email veya şifre hatalı." });

            if (string.IsNullOrWhiteSpace(user.PasswordSalt))
                return Unauthorized(new { message = "Bu hesabın şifresi geçersiz formatta. Lütfen yeniden kayıt/şifre yenile." });

            bool ok = PasswordHasher.Verify(
                password: req.Password,
                saltB64: user.PasswordSalt,
                expectedHashB64: user.PasswordHash
            );

            if (!ok)
                return Unauthorized(new { message = "Email veya şifre hatalı." });

            if (!user.IsActive)
                return Unauthorized(new { message = "Hesap pasif." });

            var now = DateTime.UtcNow;

            var openSessions = await _context.UserSessionActivities
                .Where(x => x.UserId == user.Id && x.LogoutAt == null)
                .ToListAsync();

            foreach (var open in openSessions)
                open.LogoutAt = open.LastSeenAt > open.LoginAt ? open.LastSeenAt : now;

            _context.UserSessionActivities.Add(new UserSessionActivity
            {
                UserId = user.Id,
                LoginAt = now,
                LastSeenAt = now,
                LogoutAt = null
            });

            user.LastLogin = now;
            await _context.SaveChangesAsync();

            var token = CreateJwtToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name ?? ""
            });
        }




        private string CreateJwtToken(User user)
        {
            var jwt = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("Jwt");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? "")
        };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}