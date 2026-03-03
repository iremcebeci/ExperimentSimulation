using ExperimentSimulation.DataAccessLayer.Concrete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExperimentSimulation.WebApi.Security;
using System.Text;

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

            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new LoginResponse
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name ?? ""
            });
        }
    }
}