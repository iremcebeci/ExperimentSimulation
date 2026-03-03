namespace ExperimentSimulation.WebApi.Dtos
{
    public class CreateUserDto
    {
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int RoleId { get; set; } = 1;
        public bool IsActive { get; set; } = true;

        public string? Phone { get; set; }
        public List<string> ClassCodes { get; set; } = new();
        public string? BirthDate { get; set; }
    }
}
