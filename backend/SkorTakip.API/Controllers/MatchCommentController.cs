using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Data;
using SkorTakip.API.Hubs;
using SkorTakip.API.Models;
using System.Security.Claims;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchCommentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<MatchHub> _hubContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public MatchCommentController(
        ApplicationDbContext context,
        IHubContext<MatchHub> hubContext,
        UserManager<ApplicationUser> userManager)
    {
        _context    = context;
        _hubContext = hubContext;
        _userManager = userManager;
    }

    /// <summary>
    /// Belirtilen maça ait son 100 yorumu döner. Herkese açık.
    /// </summary>
    [HttpGet("{matchId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(string matchId)
    {
        var comments = await _context.MatchComments
            .Where(c => c.MatchId == matchId)
            .OrderBy(c => c.CreatedAt)
            .Take(100)
            .Include(c => c.User)
            .Select(c => new
            {
                id        = c.Id,
                matchId   = c.MatchId,
                userId    = c.UserId,
                userName  = c.User.UserName,
                content   = c.Content,
                createdAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(comments);
    }

    /// <summary>
    /// Yorum ekler. Sadece giriş yapmış ve sohbet yasağı olmayan kullanıcılar.
    /// </summary>
    [HttpPost]
    [Authorize]
    [EnableRateLimiting("comments")]
    public async Task<IActionResult> AddComment([FromBody] AddCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 500)
            return BadRequest(new { message = "Yorum 1-500 karakter arasında olmalıdır." });

        if (string.IsNullOrWhiteSpace(request.MatchId))
            return BadRequest(new { message = "MatchId gereklidir." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var isBanned = await _context.ChatBans.AnyAsync(b => b.UserId == userId);
        if (isBanned)
            return StatusCode(403, new { message = "Sohbetten yasaklandınız." });

        var comment = new MatchComment
        {
            MatchId   = request.MatchId,
            UserId    = userId,
            Content   = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.MatchComments.Add(comment);
        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId);

        var dto = new
        {
            id        = comment.Id,
            matchId   = comment.MatchId,
            userId    = comment.UserId,
            userName  = user?.UserName ?? "Bilinmiyor",
            content   = comment.Content,
            createdAt = comment.CreatedAt
        };

        await _hubContext.Clients
            .Group($"match-{comment.MatchId}")
            .SendAsync("NewComment", dto);

        return Ok(dto);
    }

    /// <summary>
    /// Yorumu siler. Kullanıcı kendi yorumunu, admin herkesinkini silebilir.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await _context.MatchComments.FindAsync(id);
        if (comment == null) return NotFound();

        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && comment.UserId != userId)
            return Forbid();

        var matchId = comment.MatchId;
        _context.MatchComments.Remove(comment);
        await _context.SaveChangesAsync();

        await _hubContext.Clients
            .Group($"match-{matchId}")
            .SendAsync("CommentDeleted", id);

        return Ok(new { id });
    }
}

public record AddCommentRequest(string MatchId, string Content);
