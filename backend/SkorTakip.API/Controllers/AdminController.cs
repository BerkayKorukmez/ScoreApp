using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Data;
using SkorTakip.API.Hubs;
using SkorTakip.API.Models;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IExternalApiService _externalApiService;
    private readonly IHubContext<MatchHub> _hubContext;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IExternalApiService externalApiService,
        IHubContext<MatchHub> hubContext)
    {
        _context = context;
        _userManager = userManager;
        _externalApiService = externalApiService;
        _hubContext = hubContext;
    }

    // ════════════════════════════════════════════════════════════
    //  MAÇ YÖNETİMİ — Futbol, Basketbol, Voleybol (Tenis hariç)
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Tüm maçları döner: API (futbol, basketbol, voleybol) + DB. Tenis hariç.
    /// </summary>
    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null)
    {
        var visibilityById = await _context.Matches
            .Select(m => new { m.Id, m.IsHidden })
            .ToDictionaryAsync(m => m.Id, m => m.IsHidden);

        var allMatches = new List<Match>();

        try
        {
            var footballTask = _externalApiService.FetchFootballMatchesAsync();
            var basketballTask = _externalApiService.FetchBasketballMatchesAsync();
            var volleyballTask = _externalApiService.FetchVolleyballMatchesAsync();
            await Task.WhenAll(footballTask, basketballTask, volleyballTask);

            allMatches.AddRange(await footballTask);
            allMatches.AddRange(await basketballTask);
            allMatches.AddRange(await volleyballTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Admin maç çekme hatası: {ex.Message}");
        }

        var dbMatches = await _context.Matches
            .Where(m => m.SportType != SportType.Tennis)
            .ToListAsync();

        var seenIds = new HashSet<string>();
        foreach (var m in allMatches)
            seenIds.Add(m.Id);
        foreach (var m in dbMatches)
        {
            if (!seenIds.Contains(m.Id))
            {
                allMatches.Add(m);
                seenIds.Add(m.Id);
            }
        }

        var searchLower = search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(searchLower))
        {
            allMatches = allMatches
                .Where(m =>
                    (m.HomeTeam?.ToLower().Contains(searchLower) ?? false) ||
                    (m.AwayTeam?.ToLower().Contains(searchLower) ?? false) ||
                    (m.League?.ToLower().Contains(searchLower) ?? false))
                .ToList();
        }

        var total = allMatches.Count;
        var paged = allMatches
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
                IsHidden = visibilityById.TryGetValue(m.Id, out var vis) && vis
            })
            .ToList();

        return Ok(new { total, page, pageSize, data = paged });
    }

    /// <summary>
    /// Bir maçın görünürlüğünü değiştirir (gizle / göster).
    /// Eğer maç DB'de yoksa yeni kayıt oluşturur (sadece ID + IsHidden).
    /// </summary>
    [HttpPatch("matches/{id}/visibility")]
    public async Task<IActionResult> ToggleMatchVisibility(string id, [FromBody] ToggleVisibilityRequest? body = null)
    {
        var match = await _context.Matches.FindAsync(id);

        if (match == null)
        {
            // Maç DB'de yok (API maçı) — gizlilik için kayıt oluştur (body'den veri varsa kullan)
            var homeTeam = body?.HomeTeam ?? "_";
            var awayTeam = body?.AwayTeam ?? "_";
            var league   = body?.League   ?? "_";
            match = new Match
            {
                Id         = id,
                HomeTeam   = homeTeam,
                AwayTeam   = awayTeam,
                League     = league,
                SportType  = body?.SportType ?? SportType.Football,
                Status     = body?.Status ?? MatchStatus.NotStarted,
                StartTime  = body?.StartTime ?? DateTime.UtcNow,
                IsHidden   = true
            };
            _context.Matches.Add(match);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("MatchVisibilityChanged", id, true);
            return Ok(new { id, isHidden = true });
        }

        var wasStub = match.HomeTeam == "?" || match.HomeTeam == "_";

        if (match.IsHidden)
        {
            match.IsHidden = false;
            if (wasStub && body != null)
            {
                match.HomeTeam   = body.HomeTeam ?? match.HomeTeam;
                match.AwayTeam   = body.AwayTeam ?? match.AwayTeam;
                match.League     = body.League   ?? match.League;
                match.SportType  = body.SportType ?? match.SportType;
            }
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("MatchVisibilityChanged", id, false);
            return Ok(new { id, isHidden = false });
        }
        else
        {
            match.IsHidden = true;
            if (body != null)
            {
                match.HomeTeam   = body.HomeTeam ?? match.HomeTeam;
                match.AwayTeam   = body.AwayTeam ?? match.AwayTeam;
                match.League     = body.League   ?? match.League;
            }
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("MatchVisibilityChanged", id, true);
            return Ok(new { id, isHidden = true });
        }
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

    // ════════════════════════════════════════════════════════════
    //  SOHBET YASAK YÖNETİMİ
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Sohbetten yasaklanan tüm kullanıcıları listeler.
    /// </summary>
    [HttpGet("chatbans")]
    public async Task<IActionResult> GetChatBans()
    {
        var bans = await _context.ChatBans
            .Include(b => b.User)
            .Include(b => b.BannedByAdmin)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                id              = b.Id,
                userId          = b.UserId,
                userName        = b.User.UserName,
                bannedByAdminId = b.BannedByAdminId,
                bannedByAdmin   = b.BannedByAdmin.UserName,
                createdAt       = b.CreatedAt
            })
            .ToListAsync();

        return Ok(bans);
    }

    /// <summary>
    /// Kullanıcıyı sohbetten yasaklar. Zaten yasaklıysa 409 döner.
    /// </summary>
    [HttpPost("chatban/{userId}")]
    public async Task<IActionResult> BanUserFromChat(string userId)
    {
        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (adminId == null) return Unauthorized();

        if (adminId == userId)
            return BadRequest(new { message = "Kendinizi yasaklayamazsınız." });

        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        var alreadyBanned = await _context.ChatBans.AnyAsync(b => b.UserId == userId);
        if (alreadyBanned)
            return Conflict(new { message = "Kullanıcı zaten yasaklı." });

        var ban = new ChatBan
        {
            UserId          = userId,
            BannedByAdminId = adminId,
            CreatedAt       = DateTime.UtcNow
        };
        _context.ChatBans.Add(ban);
        await _context.SaveChangesAsync();

        // Kullanıcının açık bağlantısına anlık bildirim gönder
        await _hubContext.Clients.User(userId).SendAsync("ChatBanned");

        return Ok(new { message = $"{targetUser.UserName} sohbetten yasaklandı." });
    }

    /// <summary>
    /// Kullanıcının sohbet yasağını kaldırır.
    /// </summary>
    [HttpDelete("chatban/{userId}")]
    public async Task<IActionResult> UnbanUserFromChat(string userId)
    {
        var ban = await _context.ChatBans.FirstOrDefaultAsync(b => b.UserId == userId);
        if (ban == null) return NotFound(new { message = "Bu kullanıcıya ait yasak bulunamadı." });

        _context.ChatBans.Remove(ban);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.User(userId).SendAsync("ChatUnbanned");

        return Ok(new { message = "Yasak kaldırıldı." });
    }
}

public record ResetPasswordRequest(string NewPassword);

public record ToggleVisibilityRequest(
    string? HomeTeam,
    string? AwayTeam,
    string? League,
    Models.SportType? SportType,
    MatchStatus? Status,
    DateTime? StartTime
);
