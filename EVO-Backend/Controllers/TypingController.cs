using EVO_Backend.Data;
using EVO_Backend.Dtos.Typing;
using EVO_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EVO_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TypingController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeProvider _time;

    public TypingController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, TimeProvider time)
    {
        _db = db; _userManager = userManager; _time = time;
    }

    // GET /api/typing/leaderboard  -> أعلى 5
    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard()
    {
        var top = await _db.TypingScores
            .OrderByDescending(t => t.BestWpm)
            .ThenByDescending(t => t.UpdatedAtUtc) // عند التعادل: الأحدث أعلى
            .Take(5)
            .Select(t => new { name = t.User!.FullName, wpm = t.BestWpm })
            .ToListAsync();

        return Ok(top);
    }

    // GET /api/typing/me  -> نتيجتي
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> MyBest()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var score = await _db.TypingScores.FirstOrDefaultAsync(x => x.UserId == user.Id);
        return Ok(new { wpm = score?.BestWpm ?? 0 });
    }

    // POST /api/typing/submit  -> يحدّث أفضل نتيجة فقط
    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> Submit([FromBody] SubmitScoreDto dto)
    {
        // حماية بسيطة ضد القيم غير المنطقية
        if (dto.Wpm <= 0 || dto.Wpm > 400) return BadRequest("Invalid WPM.");

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var now = _time.GetUtcNow().UtcDateTime;
        var existing = await _db.TypingScores.FirstOrDefaultAsync(x => x.UserId == user.Id);

        if (existing is null)
        {
            _db.TypingScores.Add(new TypingScore { Id = Guid.NewGuid(), UserId = user.Id, BestWpm = dto.Wpm, UpdatedAtUtc = now });
        }
        else
        {
            if (dto.Wpm > existing.BestWpm)
            {
                existing.BestWpm = dto.Wpm;
                existing.UpdatedAtUtc = now;
            }
            else if (dto.Wpm == existing.BestWpm)
            {
                // في حالة التعادل بدك آخر واحد يكون أعلى ترتيب
                existing.UpdatedAtUtc = now;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Saved.", best = existing?.BestWpm ?? dto.Wpm });
    }
}
