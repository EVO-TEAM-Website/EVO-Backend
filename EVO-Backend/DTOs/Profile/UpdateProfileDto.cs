// Dtos/Profile/UpdateProfileDto.cs
namespace EVO_Backend.Dtos.Profile
{
    public class UpdateProfileDto
    {
        public string FullName { get; set; } = "";
        public string Specialization { get; set; } = "CS"; // CS, CIS, AI, BIT, SE, CSY
    }
}

// Dtos/Profile/ChangePasswordDto.cs
namespace EVO_Backend.Dtos.Profile
{
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmNewPassword { get; set; } = "";
    }
}
