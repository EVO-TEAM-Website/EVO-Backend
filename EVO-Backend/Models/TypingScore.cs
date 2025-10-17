// Models/TypingScore.cs
namespace EVO_Backend.Models
{
    public class TypingScore
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int BestWpm { get; set; }
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
    }
}
