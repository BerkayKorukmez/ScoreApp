using Microsoft.AspNetCore.Mvc;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FixtureController : ControllerBase
{
    private readonly IExternalApiService _api;

    public FixtureController(IExternalApiService api)
    {
        _api = api;
    }

    // ── Takım / lig arama ────────────────────────────────────────────────────
    // GET /api/fixture/search?query=galatasaray&sport=football&kind=team
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] string sport = "football",
        [FromQuery] string kind  = "team")
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("query parametresi zorunludur.");

        var results = (sport.ToLower(), kind.ToLower()) switch
        {
            ("football",   "team")   => await _api.SearchFootballTeamsAsync(query),
            ("football",   "league") => await _api.SearchFootballLeaguesAsync(query),
            ("basketball", "team")   => await _api.SearchBasketballTeamsAsync(query),
            ("basketball", "league") => await _api.SearchBasketballLeaguesAsync(query),
            ("volleyball", "team")   => await _api.SearchVolleyballTeamsAsync(query),
            ("volleyball", "league") => await _api.SearchVolleyballLeaguesAsync(query),
            _                        => []
        };

        return Ok(results);
    }

    // ── Takıma göre fikstür ──────────────────────────────────────────────────
    // GET /api/fixture/team?teamId=157&season=2025&sport=football
    [HttpGet("team")]
    public async Task<IActionResult> GetByTeam(
        [FromQuery] int    teamId,
        [FromQuery] string sport  = "football",
        [FromQuery] int?   season = null)
    {
        var now            = DateTime.UtcNow;
        // Futbol sezonu: ocak-temmuz → önceki yıl, ağustos-aralık → bu yıl
        var computed         = now.Month < 8 ? now.Year - 1 : now.Year;
        var footballSeason   = season ?? computed;
        var basketballSeason = $"{footballSeason}-{footballSeason + 1}";
        var volSeason        = season ?? computed;

        var matches = sport.ToLower() switch
        {
            "basketball" => await _api.FetchBasketballFixturesByTeamAsync(teamId, basketballSeason),
            "volleyball" => await _api.FetchVolleyballFixturesByTeamAsync(teamId, volSeason),
            _            => await _api.FetchFootballFixturesByTeamAsync(teamId, footballSeason)
        };

        return Ok(matches.OrderBy(m => m.StartTime));
    }

    // ── Lige göre fikstür ────────────────────────────────────────────────────
    // GET /api/fixture/league?leagueId=203&season=2025&sport=football
    [HttpGet("league")]
    public async Task<IActionResult> GetByLeague(
        [FromQuery] int    leagueId,
        [FromQuery] string sport  = "football",
        [FromQuery] int?   season = null)
    {
        var now              = DateTime.UtcNow;
        var computed         = now.Month < 8 ? now.Year - 1 : now.Year;
        var footballSeason   = season ?? computed;
        var basketballSeason = $"{footballSeason}-{footballSeason + 1}";
        var volSeason        = season ?? computed;

        var matches = sport.ToLower() switch
        {
            "basketball" => await _api.FetchBasketballFixturesByLeagueAsync(leagueId, basketballSeason),
            "volleyball" => await _api.FetchVolleyballFixturesByLeagueAsync(leagueId, volSeason),
            _            => await _api.FetchFootballFixturesByLeagueAsync(leagueId, footballSeason)
        };

        return Ok(matches.OrderBy(m => m.StartTime));
    }
}
