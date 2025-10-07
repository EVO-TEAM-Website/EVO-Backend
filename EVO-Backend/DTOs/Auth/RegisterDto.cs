// Dtos/Auth/RegisterDto.cs
namespace EVO_Backend.Dtos.Auth
{
    public class RegisterDto
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
        public string Specialization { get; set; } = "CS"; // CS, CIS, AI, BIT, SE, CSY
    }
}

