using ExperimentSimulation.BusinessLayer.Abstract;
using ExperimentSimulation.DataAccessLayer.Concrete;
using ExperimentSimulation.EntityLayer.Concrete;
using ExperimentSimulation.WebApi.Dtos;
using ExperimentSimulation.WebApi.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExperimentSimulation.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        [HttpPost]
        public IActionResult AddUser(CreateUserDto dto)
        {

            bool exists = _context.Users.Any(u => u.Email == dto.Email);
            if (exists) return BadRequest("Bu email zaten kayıtlı.");


            var (hashB64, saltB64) = PasswordHasher.HashPassword(dto.Password);

            DateTime? birthDate = null;
            if (!string.IsNullOrWhiteSpace(dto.BirthDate))
            {

                if (DateTime.TryParse(dto.BirthDate, out var bd))
                    birthDate = bd;
                else
                    return BadRequest("BirthDate formatı geçersiz.");
            }

            var user = new User
            {
                Name = dto.Name.Trim(),
                Surname = dto.Surname.Trim(),
                Email = dto.Email.Trim(),

                PasswordHash = hashB64,
                PasswordSalt = saltB64,

                RoleId = dto.RoleId,
                IsActive = dto.IsActive,

                Phone = dto.Phone,
                BirthDate = birthDate,

                CreatedAt = DateTime.UtcNow,
                Role = null
            };

            _userService.TInsert(user);
            return Ok(new { message = "User created" });
        }

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
    }
}