using Microsoft.AspNetCore.Mvc;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Controllers;

/// <summary>
/// Puan tablosu endpoint'leri.
/// Route, frontend uyumluluğu için /api/match/standings/* olarak korunmuştur.
/// </summary>
[ApiController]
[Route("api/match/standings")]
public class StandingsController : ControllerBase
{
    private readonly IExternalApiService _externalApiService;

    public StandingsController(IExternalApiService externalApiService)
    {
        _externalApiService = externalApiService;
    }

    /// <summary>
    /// Futbol ligleri için puan durumu döner.
    /// collectApiKey varsa CollectAPI kullanılır (Süper Lig, Premier League vb. yerli ligler).
    /// collectApiKey yoksa leagueId ile API-Sports kullanılır (UCL, UEL, Nations League vb.).
    /// </summary>
    [HttpGet("football")]
    public async Task<IActionResult> GetFootballStandings(
        [FromQuery] string?  collectApiKey,
        [FromQuery] int?     leagueId,
        [FromQuery] int?     season)
    {
        // CollectAPI yolu — yerli ligler
        if (!string.IsNullOrWhiteSpace(collectApiKey))
        {
            var collectStandings = await _externalApiService.FetchFootballStandingsFromCollectApiAsync(collectApiKey);
            return Ok(collectStandings);
        }

        // API-Sports yolu — Avrupa kupaları, milli takım turnuvaları
        if (leagueId is null or <= 0)
            return BadRequest("collectApiKey veya leagueId parametresi gereklidir.");

        var now            = DateTime.UtcNow;
        var computed       = now.Month >= 8 ? now.Year : now.Year - 1;
        var resolvedSeason = season ?? Math.Min(computed, 2024);

        var standings = await _externalApiService.FetchFootballStandingsAsync(leagueId.Value, resolvedSeason);
        return Ok(standings);
    }

    /// <summary>
    /// Basketbol ligleri için puan durumu döner (API-Basketball üzerinden).
    /// season parametresi "2024-2025" formatında olmalıdır; verilmezse otomatik hesaplanır.
    /// </summary>
    [HttpGet("basketball")]
    public async Task<IActionResult> GetBasketballStandings(
        [FromQuery] int leagueId,
        [FromQuery] string? season)
    {
        if (leagueId <= 0)
            return BadRequest("Gecersiz lig ID.");

        var now            = DateTime.UtcNow;
        var computedYear   = now.Month >= 10 ? now.Year : now.Year - 1;
        var resolvedSeason = season ?? $"{computedYear}-{computedYear + 1}";

        var standings = await _externalApiService.FetchBasketballStandingsAsync(leagueId, resolvedSeason);
        return Ok(standings);
    }

    /// <summary>
    /// Voleybol ligleri için puan durumu döner (API-Volleyball üzerinden).
    /// Sezon parametresi verilmezse mevcut yıl kullanılır.
    /// </summary>
    [HttpGet("volleyball")]
    public async Task<IActionResult> GetVolleyballStandings(
        [FromQuery] int leagueId,
        [FromQuery] int? season)
    {
        if (leagueId <= 0)
            return BadRequest("Gecersiz lig ID.");

        var now            = DateTime.UtcNow;
        var resolvedSeason = season ?? now.Year;

        var standings = await _externalApiService.FetchVolleyballStandingsAsync(leagueId, resolvedSeason);
        return Ok(standings);
    }
}
