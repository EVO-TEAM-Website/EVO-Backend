namespace EVO_Backend.Models
{
    public enum CourseCategory { Development = 1, Design = 2, Business = 3 }
    public enum CourseDifficulty { Beginner = 1, Intermediate = 2, Advanced = 3 }

    public class Course
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Instructor { get; set; } = "";
        public CourseCategory Category { get; set; }
        public CourseDifficulty Difficulty { get; set; }
        public string ImageUrl { get; set; } = "";      // رابط صورة
        public string ExternalLink { get; set; } = "";  // رابط الكورس (يوتيوب/منصة)
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
