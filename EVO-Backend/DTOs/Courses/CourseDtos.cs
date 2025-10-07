using EVO_Backend.Models;

namespace EVO_Backend.Dtos.Courses
{
    public class CreateCourseDto
    {
        public string Title { get; set; } = "";
        public string Instructor { get; set; } = "";
        public string Category { get; set; } = "";     // "Development" | "Design" | "Business"
        public string Difficulty { get; set; } = "";   // "Beginner" | "Intermediate" | "Advanced"
        public string ImageUrl { get; set; } = "";
        public string ExternalLink { get; set; } = "";
    }

    public class UpdateCourseDto : CreateCourseDto { }

    public class CourseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Instructor { get; set; } = "";
        public string Category { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string ExternalLink { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
    }

    public static class CourseMapping
    {
        public static CourseDto ToDto(this Course c) => new()
        {
            Id = c.Id,
            Title = c.Title,
            Instructor = c.Instructor,
            Category = c.Category.ToString(),
            Difficulty = c.Difficulty.ToString(),
            ImageUrl = c.ImageUrl,
            ExternalLink = c.ExternalLink,
            CreatedAtUtc = c.CreatedAtUtc
        };
    }
}
