using Microsoft.AspNetCore.Mvc;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/player")]
public class PlayerController : ControllerBase
{
    private readonly IExternalApiService _externalApiService;

    public PlayerController(IExternalApiService externalApiService)
    {
        _externalApiService = externalApiService;
    }

    /// <summary>
    /// Oyuncu profili: biyografi + sezon istatistikleri.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPlayerProfile(int id, [FromQuery] int? season)
    {
        if (id <= 0) return BadRequest("Geçersiz oyuncu ID.");

        var now      = DateTime.UtcNow;
        var computed = now.Month >= 8 ? now.Year : now.Year - 1;
        var resolvedSeason = season ?? computed;

        var profile = await _externalApiService.FetchPlayerProfileAsync(id, resolvedSeason);
        if (profile is null) return NotFound("Oyuncu bulunamadı.");
        return Ok(profile);
    }
}
