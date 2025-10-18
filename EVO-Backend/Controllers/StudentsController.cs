using EVO_Backend.Data;
using EVO_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EVO_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class StudentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDir = "asc")
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var studentsQuery =
            from u in _userManager.Users.AsNoTracking()
            join ur in _db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where r.Name == "Student"
            select new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Specialization,
                u.EmailConfirmed,
                u.CreatedAtUtc
            };

        // فلترة بالاسم/الإيميل
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            studentsQuery = studentsQuery.Where(s =>
                (s.FullName ?? "").ToLower().Contains(term) ||
                (s.Email ?? "").ToLower().Contains(term));
        }

        // ترتيب
        bool desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        studentsQuery = (sortBy.ToLower()) switch
        {
            "email" => (desc ? studentsQuery.OrderByDescending(s => s.Email) : studentsQuery.OrderBy(s => s.Email)),
            "createdat" => (desc ? studentsQuery.OrderByDescending(s => s.CreatedAtUtc) : studentsQuery.OrderBy(s => s.CreatedAtUtc)),
            _ => (desc ? studentsQuery.OrderByDescending(s => s.FullName) : studentsQuery.OrderBy(s => s.FullName)),
        };

        var total = await studentsQuery.CountAsync();

        var items = await studentsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            items
        });
    }



    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var res = await _userManager.DeleteAsync(user);
        if (!res.Succeeded) return BadRequest(res.Errors);

        return NoContent();
    }
}
