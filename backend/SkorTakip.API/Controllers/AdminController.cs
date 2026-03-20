using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Data;
using SkorTakip.API.Models;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ════════════════════════════════════════════════════════════
    //  MAÇ YÖNETİMİ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// DB'deki tüm maçları döner (gizliler dahil). Admin paneli için.
    /// </summary>
    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null)
    {
        var query = _context.Matches.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(m =>
                m.HomeTeam.ToLower().Contains(search) ||
                m.AwayTeam.ToLower().Contains(search) ||
                m.League.ToLower().Contains(search));
        }

        var total = await query.CountAsync();
        var matches = await query
            .OrderByDescending(m => m.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.HomeTeam,
                m.AwayTeam,
                m.HomeTeamLogo,
                m.AwayTeamLogo,
                m.HomeScore,
                m.AwayScore,
                m.League,
                m.LeagueCountry,
                m.SportType,
                m.Status,
                m.StartTime,
                m.IsHidden
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = matches });
    }

    /// <summary>
    /// Bir maçın görünürlüğünü değiştirir (gizle / göster).
    /// Eğer maç DB'de yoksa yeni kayıt oluşturur (sadece ID + IsHidden).
    /// </summary>
    [HttpPatch("matches/{id}/visibility")]
    public async Task<IActionResult> ToggleMatchVisibility(string id)
    {
        var match = await _context.Matches.FindAsync(id);

        if (match == null)
        {
            // Maç DB'de yok (live/API maçı) — stub kayıt oluştur
            match = new Match
            {
                Id       = id,
                HomeTeam = "?",
                AwayTeam = "?",
                League   = "?",
                IsHidden = true
            };
            _context.Matches.Add(match);
            await _context.SaveChangesAsync();
            return Ok(new { id, isHidden = true });
        }

        match.IsHidden = !match.IsHidden;
        await _context.SaveChangesAsync();

        return Ok(new { id, isHidden = match.IsHidden });
    }

    // ════════════════════════════════════════════════════════════
    //  KULLANICI YÖNETİMİ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Tüm kullanıcıları listeler (rol bilgisiyle birlikte).
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u =>
                u.UserName!.ToLower().Contains(search) ||
                u.Email!.ToLower().Contains(search));
        }

        var total = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Rolleri toplu getir
        var result = new List<object>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                IsAdmin = roles.Contains("Admin"),
                Roles = roles
            });
        }

        return Ok(new { total, page, pageSize, data = result });
    }

    /// <summary>
    /// Bir kullanıcının şifresini sıfırlar.
    /// </summary>
    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { message = "Şifre en az 6 karakter olmalıdır." });

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Şifre başarıyla sıfırlandı." });
    }

    /// <summary>
    /// Bir kullanıcıyı siler (kendini silemez).
    /// </summary>
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (id == currentUserId)
            return BadRequest(new { message = "Kendi hesabınızı silemezsiniz." });

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Kullanıcı silindi." });
    }

    /// <summary>
    /// Bir kullanıcıya admin rolü verir ya da alır.
    /// </summary>
    [HttpPost("users/{id}/toggle-admin")]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (id == currentUserId)
            return BadRequest(new { message = "Kendi admin rolünüzü değiştiremezsiniz." });

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin)
        {
            await _userManager.RemoveFromRoleAsync(user, "Admin");
            return Ok(new { id, isAdmin = false, message = "Admin rolü kaldırıldı." });
        }
        else
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            return Ok(new { id, isAdmin = true, message = "Admin rolü verildi." });
        }
    }
}

public record ResetPasswordRequest(string NewPassword);
