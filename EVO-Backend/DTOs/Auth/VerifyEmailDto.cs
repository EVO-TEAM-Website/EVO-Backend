// Dtos/Auth/VerifyEmailDto.cs
namespace EVO_Backend.Dtos.Auth
{
    public class VerifyEmailDto
    {
        public string Email { get; set; } = "";
        public string Code { get; set; } = ""; // 6 أرقام
    }
}