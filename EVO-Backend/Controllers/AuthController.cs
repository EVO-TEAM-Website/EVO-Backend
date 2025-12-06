using System.Security.Claims;
using EVO_Backend.Data;
using EVO_Backend.Dtos.Auth;
using EVO_Backend.Models;
using EVO_Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Linq;

namespace EVO_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _email;
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;

    public AuthController(UserManager<ApplicationUser> userManager, IEmailService email, ApplicationDbContext db, TimeProvider time)
    {
        _userManager = userManager;
        _email = email;
        _db = db;
        _time = time;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword) return BadRequest("Passwords do not match.");
        if (!Enum.TryParse<Specialization>(dto.Specialization, true, out var spec))
            return BadRequest("Invalid specialization.");

        var existing = await _userManager.FindByEmailAsync(dto.Email);
        var now = _time.GetUtcNow().UtcDateTime;

        // 1) موجود وغير مؤكَّد → أعد إرسال كود جديد
        if (existing is not null && !existing.EmailConfirmed)
        {
            var code = Random.Shared.Next(100000, 999999).ToString();
            var ev = await _db.EmailVerifications.SingleOrDefaultAsync(x => x.UserId == existing.Id);
            if (ev is null) { ev = new EmailVerification { UserId = existing.Id }; _db.EmailVerifications.Add(ev); }
            ev.Code = code;
            ev.ExpiresAtUtc = now.AddMinutes(10);
            ev.Attempts = 0;
            ev.LastSentAtUtc = now;
            await _db.SaveChangesAsync();

            try
            {
                await _email.SendAsync(existing.Email!, "Verify your email", $"<p>Your code is <b>{code}</b></p>");

            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Ok(new { message = "User exists but not verified. New code sent." });
        }

        // 2) موجود ومؤكَّد → رجّع Duplicate
        if (existing is not null) return BadRequest(new[] {
        new { code="DuplicateEmail", description=$"Email '{dto.Email}' is already taken." }
    });

        // 3) أنشئ مستخدم جديد
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.Email,
            Specialization = spec
        };

        var create = await _userManager.CreateAsync(user, dto.Password);
        if (!create.Succeeded) return BadRequest(create.Errors);
        await _userManager.AddToRoleAsync(user, "Student");

        // 4) أرسل الكود، ولو فشل احذف المستخدم
        try
        {
            var code = Random.Shared.Next(100000, 999999).ToString();
            var ev = new EmailVerification
            {
                UserId = user.Id,
                Code = code,
                ExpiresAtUtc = now.AddMinutes(10),
                Attempts = 0,
                LastSentAtUtc = now
            };
            _db.EmailVerifications.Add(ev);
            await _db.SaveChangesAsync();

            await _email.SendAsync(user.Email!, "Verify your email", $"<p>Your code is <b>{code}</b></p>");
            return Ok(new { message = "Registered. Verification code sent to email." });
        }
        catch
        {
            // rollback
            await _userManager.DeleteAsync(user);
            await _db.SaveChangesAsync();
            return StatusCode(500, "Registration failed while sending email. Please try again.");
        }
    }


    [HttpPost("resend-code")]
    public async Task<IActionResult> ResendCode(ResendCodeDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return NotFound("User not found.");
        if (user.EmailConfirmed) return BadRequest("Email already confirmed.");

        var ev = await _db.EmailVerifications.SingleOrDefaultAsync(x => x.UserId == user.Id);
        var now = _time.GetUtcNow().UtcDateTime;

        if (ev is not null && ev.LastSentAtUtc.HasValue && (now - ev.LastSentAtUtc.Value).TotalSeconds < 60)
            return BadRequest("Please wait before requesting another code.");

        var code = Random.Shared.Next(100000, 999999).ToString();
        if (ev is null) { ev = new EmailVerification { UserId = user.Id }; _db.EmailVerifications.Add(ev); }
        ev.Code = code;
        ev.ExpiresAtUtc = now.AddMinutes(10);
        ev.Attempts = 0;
        ev.LastSentAtUtc = now;
        await _db.SaveChangesAsync();

        await _email.SendAsync(user.Email!, "Verify your email", $"<p>Your code is <b>{code}</b>. It expires in 10 minutes.</p>");
        return Ok(new { message = "Code re-sent." });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return NotFound("User not found.");
        if (user.EmailConfirmed) return BadRequest("Email already confirmed.");

        var ev = await _db.EmailVerifications.SingleOrDefaultAsync(x => x.UserId == user.Id);
        if (ev is null) return BadRequest("No active code. Please request a new one.");

        var now = _time.GetUtcNow().UtcDateTime;
        if (ev.ExpiresAtUtc < now) return BadRequest("Code expired.");
        if (ev.Attempts >= 5) return BadRequest("Too many attempts. Request a new code.");

        if (ev.Code != dto.Code)
        {
            ev.Attempts++;
            await _db.SaveChangesAsync();
            return BadRequest("Invalid code.");
        }

        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        _db.EmailVerifications.Remove(ev);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Email verified." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return Unauthorized("Invalid credentials.");

        // تأكد من الإيميل مؤكد
        if (!user.EmailConfirmed)
            return BadRequest("Email is not verified.");

        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized("Invalid credentials.");

        // أدوار المستخدم
        var roles = await _userManager.GetRolesAsync(user);

        // إعدادات JWT
        var jwt = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetSection("Jwt");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.FullName ?? "")
    };
        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiresMinutes"]!)),
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token = accessToken,
            expiresIn = int.Parse(jwt["ExpiresMinutes"]!) * 60,
            user = new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                roles
            }
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        // لا نكشف وجود الحساب لأسباب أمان
        if (user == null || !user.EmailConfirmed)
            return Ok(new { message = "If the account exists, a reset code was sent." });

        var now = _time.GetUtcNow().UtcDateTime;
        var ev = await _db.EmailVerifications.SingleOrDefaultAsync(x => x.UserId == user.Id);

        if (ev is not null && ev.LastSentAtUtc.HasValue && (now - ev.LastSentAtUtc.Value).TotalSeconds < 60)
            return BadRequest("Please wait before requesting another code.");

        var code = Random.Shared.Next(100000, 999999).ToString();

        if (ev is null) { ev = new EmailVerification { UserId = user.Id }; _db.EmailVerifications.Add(ev); }
        ev.Code = code;
        ev.ExpiresAtUtc = now.AddMinutes(10);
        ev.Attempts = 0;
        ev.LastSentAtUtc = now;
        await _db.SaveChangesAsync();

        await _email.SendAsync(user.Email!, "Reset your password",
            $"<p>Your reset code is <b>{code}</b>. It expires in 10 minutes.</p>");

        return Ok(new { message = "If the account exists, a reset code was sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest("Passwords do not match.");

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return BadRequest("Invalid code.");

        var ev = await _db.EmailVerifications.SingleOrDefaultAsync(x => x.UserId == user.Id);
        if (ev is null) return BadRequest("No active code. Please request a new one.");

        var now = _time.GetUtcNow().UtcDateTime;
        if (ev.ExpiresAtUtc < now) return BadRequest("Code expired.");
        if (ev.Attempts >= 5) return BadRequest("Too many attempts. Request a new code.");

        var submitted = (dto.Code ?? "").Trim();
        if (submitted.Length != 6 || !submitted.All(char.IsDigit))
            return BadRequest("Invalid code format.");

        if (!string.Equals(ev.Code?.Trim(), submitted, StringComparison.Ordinal))
        {
            ev.Attempts++;
            await _db.SaveChangesAsync();
            return BadRequest("Invalid code.");
        }

        // نولّد توكن داخليًا وننفّذ Reset
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
        if (!result.Succeeded) return BadRequest(result.Errors);

        _db.EmailVerifications.Remove(ev);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password reset successful." });
    }
}
