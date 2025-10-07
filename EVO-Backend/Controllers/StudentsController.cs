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
        _db = db; _userManager = userManager;
    }

    // DELETE /api/students/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        // حذف كامل؛ العلاقات (TypingScores) حتتم Cascade
        var res = await _userManager.DeleteAsync(user);
        if (!res.Succeeded) return BadRequest(res.Errors);

        return NoContent();
    }
}
