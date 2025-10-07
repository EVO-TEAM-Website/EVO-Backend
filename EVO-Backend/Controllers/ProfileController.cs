using System.Security.Claims;
using EVO_Backend.Data;
using EVO_Backend.Dtos.Profile;
using EVO_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EVO_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // كل العمليات تتطلب JWT صالح
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeProvider _time;

    public ProfileController(UserManager<ApplicationUser> userManager, TimeProvider time)
    {
        _userManager = userManager;
        _time = time;
    }

    // GET /api/profile/me
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new
        {
            id = user.Id,
            fullName = user.FullName,
            email = user.Email,
            specialization = user.Specialization.ToString(),
            createdAtUtc = user.CreatedAtUtc,
            updatedAtUtc = user.UpdatedAtUtc,
            roles
        });
    }

    // PUT /api/profile
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        if (!Enum.TryParse<Specialization>(dto.Specialization, true, out var spec))
            return BadRequest("Invalid specialization.");

        user.FullName = dto.FullName?.Trim() ?? user.FullName;
        user.Specialization = spec;
        user.UpdatedAtUtc = _time.GetUtcNow().UtcDateTime;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { message = "Profile updated." });
    }

    // PUT /api/profile/change-password
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmNewPassword)
            return BadRequest("New passwords do not match.");

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { message = "Password changed." });
    }
}
