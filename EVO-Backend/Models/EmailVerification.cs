// Models/EmailVerification.cs
namespace EVO_Backend.Models
{
    public class EmailVerification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Code { get; set; } = "";           // 6 digits
        public DateTime ExpiresAtUtc { get; set; }       // صلاحية 10 دقائق
        public int Attempts { get; set; } = 0;           // حد أقصى 5
        public DateTime? LastSentAtUtc { get; set; }     // لإعادة الإرسال: كل 60 ثانية
    }
}
