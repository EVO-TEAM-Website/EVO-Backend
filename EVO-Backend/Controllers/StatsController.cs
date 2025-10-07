using EVO_Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EVO_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public StatsController(ApplicationDbContext db) => _db = db;

    // GET /api/stats
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // عدد الطلاب المؤكدين فقط
        var totalStudents = await (from u in _db.Users
                                   join ur in _db.UserRoles on u.Id equals ur.UserId
                                   join r in _db.Roles on ur.RoleId equals r.Id
                                   where r.Name == "Student" && u.EmailConfirmed
                                   select u.Id).Distinct().CountAsync();

        return Ok(new { totalStudents });
    }
}
