using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Data;
using SkorTakip.API.DTOs;
using SkorTakip.API.Models;
using System.Security.Claims;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoriteMatchesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FavoriteMatchesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetFavoriteMatches()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı." });

            var userId = userIdClaim.Value;

            var favoriteMatchIds = await _context.FavoriteMatches
                .Where(fm => fm.UserId == userId)
                .Select(fm => fm.MatchId)
                .ToListAsync();

            return Ok(favoriteMatchIds);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Favori maçlar yüklenirken hata oluştu.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddFavoriteMatch([FromBody] AddFavoriteMatchRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        var userId = userIdClaim.Value;

        if (string.IsNullOrWhiteSpace(request.MatchId))
            return BadRequest(new { message = "Maç ID boş olamaz." });

        var exists = await _context.FavoriteMatches
            .AnyAsync(fm => fm.UserId == userId && fm.MatchId == request.MatchId);

        if (exists)
            return BadRequest(new { message = "Bu maç zaten favorilerinizde." });

        var favoriteMatch = new FavoriteMatch
        {
            UserId = userId,
            MatchId = request.MatchId,
            AddedAt = DateTime.UtcNow
        };

        _context.FavoriteMatches.Add(favoriteMatch);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Maç favorilere eklendi.", matchId = favoriteMatch.MatchId });
    }

    [HttpDelete("{matchId}")]
    public async Task<IActionResult> RemoveFavoriteMatch(string matchId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        var userId = userIdClaim.Value;

        var favoriteMatch = await _context.FavoriteMatches
            .FirstOrDefaultAsync(fm => fm.UserId == userId && fm.MatchId == matchId);

        if (favoriteMatch == null)
            return NotFound(new { message = "Favorilerinizde bu maç bulunamadı." });

        _context.FavoriteMatches.Remove(favoriteMatch);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Maç favorilerden çıkarıldı." });
    }
}
