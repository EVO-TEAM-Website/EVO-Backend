using Microsoft.AspNetCore.Identity;

namespace EVO_Backend.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public Specialization Specialization { get; set; } = Specialization.CS;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
