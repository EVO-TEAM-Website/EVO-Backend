using EVO_Backend.Data;
using EVO_Backend.Dtos.Courses;
using EVO_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EVO_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CoursesController(ApplicationDbContext db) => _db = db;

    // GET /api/courses?q=&category=&difficulty=&page=1&pageSize=12&sortBy=createdAt|title&sortDir=desc|asc
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] string? difficulty,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDir = "desc")
    {
        var query = _db.Courses.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term) ||
                c.Instructor.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category) &&
            Enum.TryParse<CourseCategory>(category, true, out var cat))
        {
            query = query.Where(c => c.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(difficulty) &&
            Enum.TryParse<CourseDifficulty>(difficulty, true, out var diff))
        {
            query = query.Where(c => c.Difficulty == diff);
        }

        // sorting
        bool desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy.ToLower()) switch
        {
            "title" => (desc ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title)),
            _ => (desc ? query.OrderByDescending(c => c.CreatedAtUtc) : query.OrderBy(c => c.CreatedAtUtc)),
        };

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            items = items.Select(c => c.ToDto())
        });
    }

    // GET /api/courses/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var c = await _db.Courses.FindAsync(id);
        return c is null ? NotFound() : Ok(c.ToDto());
    }

    // POST /api/courses  (Admin only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCourseDto dto)
    {
        if (!Enum.TryParse<CourseCategory>(dto.Category, true, out var cat))
            return BadRequest("Invalid category. Use: Development, Design, Business.");

        if (!Enum.TryParse<CourseDifficulty>(dto.Difficulty, true, out var diff))
            return BadRequest("Invalid difficulty. Use: Beginner, Intermediate, Advanced.");

        if (string.IsNullOrWhiteSpace(dto.ImageUrl) || !Uri.IsWellFormedUriString(dto.ImageUrl, UriKind.Absolute))
            return BadRequest("ImageUrl must be a valid absolute URL.");

        if (string.IsNullOrWhiteSpace(dto.ExternalLink) || !Uri.IsWellFormedUriString(dto.ExternalLink, UriKind.Absolute))
            return BadRequest("ExternalLink must be a valid absolute URL.");

        var c = new Course
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Instructor = dto.Instructor.Trim(),
            Category = cat,
            Difficulty = diff,
            ImageUrl = dto.ImageUrl.Trim(),
            ExternalLink = dto.ExternalLink.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Courses.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = c.Id }, c.ToDto());
    }

    // PUT /api/courses/{id}  (Admin only)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseDto dto)
    {
        var c = await _db.Courses.FindAsync(id);
        if (c is null) return NotFound();

        if (!Enum.TryParse<CourseCategory>(dto.Category, true, out var cat))
            return BadRequest("Invalid category.");

        if (!Enum.TryParse<CourseDifficulty>(dto.Difficulty, true, out var diff))
            return BadRequest("Invalid difficulty.");

        if (!Uri.IsWellFormedUriString(dto.ImageUrl, UriKind.Absolute))
            return BadRequest("Invalid ImageUrl.");

        if (!Uri.IsWellFormedUriString(dto.ExternalLink, UriKind.Absolute))
            return BadRequest("Invalid ExternalLink.");

        c.Title = dto.Title.Trim();
        c.Instructor = dto.Instructor.Trim();
        c.Category = cat;
        c.Difficulty = diff;
        c.ImageUrl = dto.ImageUrl.Trim();
        c.ExternalLink = dto.ExternalLink.Trim();

        await _db.SaveChangesAsync();
        return Ok(c.ToDto());
    }

    // DELETE /api/courses/{id}  (Admin only)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await _db.Courses.FindAsync(id);
        if (c is null) return NotFound();
        _db.Courses.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
