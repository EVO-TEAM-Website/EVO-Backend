using EVO_Backend.Data;
using EVO_Backend.Models; // مكان ApplicationUser إن وجد
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EVO_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public StatsController(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    // GET /api/stats
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // كل المستخدمين في Role "Student"
            var students = await _userManager.GetUsersInRoleAsync("Student");

            // فقط المؤكدين
            var totalStudents = students.Count(u => u.EmailConfirmed);

            return Ok(new { totalStudents });
        }
        catch (Exception ex)
        {
            // رجّع رسالة مفيدة بدل 500 صامتة
            return StatusCode(500, new { error = "stats_failed", message = ex.Message });
        }
    }
}
