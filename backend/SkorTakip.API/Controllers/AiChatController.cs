using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Data;
using SkorTakip.API.Models;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiChatController : ControllerBase
{
    private readonly IAiChatService _aiChatService;
    private readonly ApplicationDbContext _context;

    public AiChatController(IAiChatService aiChatService, ApplicationDbContext context)
    {
        _aiChatService = aiChatService;
        _context = context;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Kullanıcının chat geçmişini getirir.
    /// </summary>
    [HttpGet("chat")]
    public async Task<IActionResult> GetChatHistory()
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var messages = await _context.AiChatMessages
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { role = m.Role, text = m.Text })
            .ToListAsync();

        return Ok(messages);
    }

    /// <summary>
    /// Mesaj gönderir, Gemini'den yanıt alır ve DB'ye kaydeder.
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken ct)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request?.Message))
            return BadRequest(new { message = "Mesaj boş olamaz." });

        var userMessage = request.Message.Trim();

        // Kullanıcı mesajını kaydet
        _context.AiChatMessages.Add(new AiChatMessage
        {
            UserId = userId,
            Role = "user",
            Text = userMessage
        });
        await _context.SaveChangesAsync(ct);

        var reply = await _aiChatService.SendMessageAsync(userMessage, ct);

        // Asistan yanıtını kaydet
        _context.AiChatMessages.Add(new AiChatMessage
        {
            UserId = userId,
            Role = "assistant",
            Text = reply
        });
        await _context.SaveChangesAsync(ct);

        return Ok(new { reply });
    }

    /// <summary>
    /// Kullanıcının chat geçmişini siler.
    /// </summary>
    [HttpDelete("chat")]
    public async Task<IActionResult> ClearChat()
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var toDelete = await _context.AiChatMessages.Where(m => m.UserId == userId).ToListAsync();
        _context.AiChatMessages.RemoveRange(toDelete);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Sohbet silindi." });
    }
}

public record AiChatRequest(string Message);
