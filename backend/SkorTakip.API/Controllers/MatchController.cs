using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Data;
using SkorTakip.API.DTOs;
using SkorTakip.API.Hubs;
using SkorTakip.API.Models;
using SkorTakip.API.Services;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchController : ControllerBase
{
    private readonly IHubContext<MatchHub> _hubContext;
    private readonly ApplicationDbContext _context;
    private readonly IExternalApiService _externalApiService;
    private readonly ILiveMatchService _liveMatchService;

    public MatchController(
        IHubContext<MatchHub> hubContext,
        ApplicationDbContext context,
        IExternalApiService externalApiService,
        ILiveMatchService liveMatchService)
    {
        _hubContext = hubContext;
        _context = context;
        _externalApiService = externalApiService;
        _liveMatchService = liveMatchService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllMatches(
        [FromQuery] string? league, 
        [FromQuery] MatchStatus? status,
        [FromQuery] SportType? sportType)
    {
        // Gizli maç ID'lerini önceden çek (filtre için)
        var hiddenIds = (await _context.Matches
            .Where(m => m.IsHidden)
            .Select(m => m.Id)
            .ToListAsync()).ToHashSet();

        if (sportType.HasValue)
        {
            List<Match> externalMatches = new List<Match>();
            
            try
            {
                externalMatches = sportType.Value switch
                {
                    SportType.Football => await _externalApiService.FetchFootballMatchesAsync(),
                    SportType.Basketball => await _externalApiService.FetchBasketballMatchesAsync(),
                    SportType.Volleyball => await _externalApiService.FetchVolleyballMatchesAsync(),
                    _ => new List<Match>()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"External API hatası ({sportType.Value}): {ex.Message}");
            }

            // Gizli maçları filtrele
            externalMatches = externalMatches.Where(m => !hiddenIds.Contains(m.Id)).ToList();

            var dbQuery = _context.Matches
                .Where(m => m.SportType == sportType.Value && !m.IsHidden && m.HomeTeam != "?" && m.HomeTeam != "_")
                .AsQueryable();

            if (!string.IsNullOrEmpty(league))
            {
                externalMatches = externalMatches
                    .Where(m => string.Equals(m.League, league, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                dbQuery = dbQuery.Where(m => m.League == league);
            }

            if (status.HasValue)
            {
                externalMatches = externalMatches
                    .Where(m => m.Status == status.Value)
                    .ToList();
                dbQuery = dbQuery.Where(m => m.Status == status.Value);
            }

            var dbMatchesForSport = await dbQuery.ToListAsync();

            var combined = externalMatches
                .Concat(dbMatchesForSport)
                .GroupBy(m => m.Id)
                .Select(g => g.First())
                .OrderByDescending(m => m.StartTime)
                .ToList();

            return Ok(combined);
        }

        var query = _context.Matches
            .Where(m => !m.IsHidden && m.HomeTeam != "?" && m.HomeTeam != "_")
            .AsQueryable();

        if (!string.IsNullOrEmpty(league))
            query = query.Where(m => m.League == league);

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        var dbMatches = await query.OrderByDescending(m => m.StartTime).ToListAsync();
        return Ok(dbMatches);
    }

    [HttpGet("leagues")]
    public async Task<IActionResult> GetLeagues()
    {
        var leagues = await _context.Matches
            .Select(m => m.League)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();
        return Ok(leagues);
    }

    /// <summary>
    /// Belirtilen tarih ve spor tipine göre geçmiş maçları API'den çeker.
    /// Tarih formatı: yyyy-MM-dd (örn: 2026-03-01)
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetMatchHistory(
        [FromQuery] string date,
        [FromQuery] SportType sportType = SportType.Football)
    {
        if (string.IsNullOrWhiteSpace(date) || !DateTime.TryParse(date, out var parsedDate))
            return BadRequest("Geçerli bir tarih giriniz (yyyy-MM-dd).");

        // ── Tüm sporlar için API'den çek (ücretli API — limit yok) ────────────────
        List<Match> matches;
        try
        {
            matches = sportType switch
            {
                SportType.Football   => await _externalApiService.FetchFootballMatchesByDateAsync(date),
                SportType.Basketball => await _externalApiService.FetchBasketballMatchesByDateAsync(date),
                SportType.Volleyball => await _externalApiService.FetchVolleyballMatchesByDateAsync(date),
                _ => new List<Match>()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Geçmiş maç API hatası ({sportType}): {ex.Message}");
            matches = new List<Match>();
        }

        // ── Tamamlanmış maçları DB'ye kaydet (sentetik id'leri kaydetme) ─
        // (bugün biten maçlar da dahil — yarın DB'den gelsin diye)
        if (matches.Any())
        {
            var finishedMatches = matches
                .Where(m => m.Status == MatchStatus.Finished && !m.Id.StartsWith("c-", StringComparison.Ordinal))
                .ToList();

            if (finishedMatches.Any())
            {
                var finishedIds = finishedMatches.Select(fm => fm.Id).ToList();

                var existingIds = await _context.Matches
                    .Where(m => finishedIds.Contains(m.Id))
                    .Select(m => m.Id)
                    .ToListAsync();

                var newMatches = finishedMatches
                    .Where(m => !existingIds.Contains(m.Id))
                    .GroupBy(m => m.Id)
                    .Select(g => g.First())
                    .ToList();

                if (newMatches.Any())
                {
                    try
                    {
                        _context.Matches.AddRange(newMatches);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"DB'ye kaydedildi: {newMatches.Count} yeni biten maç ({sportType}, {date})");
                    }
                    catch (DbUpdateException ex)
                    {
                        _context.ChangeTracker.Clear();
                        Console.WriteLine($"Geçmiş maç DB kaydı atlandı (DbUpdate): {ex.InnerException?.Message ?? ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        _context.ChangeTracker.Clear();
                        Console.WriteLine($"Geçmiş maç DB kaydı atlandı: {ex.Message}");
                    }
                }
            }
        }

        matches = matches.OrderByDescending(m => m.StartTime).ToList();
        return Ok(matches);
    }

    // ── Sabit route'lar {id}'den ÖNCE olmalı ─────────────────────────────────────
    [HttpGet("goalKings")]
    public async Task<IActionResult> GetGoalKings([FromQuery] int? leagueId, [FromQuery] int? season)
    {
        if (leagueId is null or <= 0)
            return BadRequest("leagueId parametresi zorunludur.");

        var now            = DateTime.UtcNow;
        var computed       = now.Month >= 8 ? now.Year : now.Year - 1;
        var resolvedSeason = season ?? computed;

        var list = await _externalApiService.FetchGoalKingsAsync(leagueId.Value, resolvedSeason);
        return Ok(list);
    }

    [HttpGet("results/football")]
    public async Task<IActionResult> GetFootballResults(
        [FromQuery] int?  leagueId,
        [FromQuery] int?  season,
        [FromQuery] int?  last)
    {
        if (leagueId is null or <= 0)
            return BadRequest("leagueId parametresi zorunludur.");

        var now            = DateTime.UtcNow;
        var computed       = now.Month >= 8 ? now.Year : now.Year - 1;
        var resolvedSeason = season ?? computed;
        var resolvedLast   = last is > 0 ? last.Value : 15;

        var results = await _externalApiService.FetchFootballRecentResultsAsync(leagueId.Value, resolvedSeason, resolvedLast);
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMatch(string id)
    {
        // Önce veritabanından dene
        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == id);
        if (match != null)
        {
            // DB maçı için de istatistik çekmeyi dene
            await TryLoadStatisticsAsync(match);
            return Ok(match);
        }

        // 1) LiveMatchService cache'inden dene
        var cachedMatch = _liveMatchService.GetCachedMatches()
            .FirstOrDefault(m => m.Id == id);

        if (cachedMatch != null)
        {
            await TryLoadStatisticsAsync(cachedMatch);
            return Ok(cachedMatch);
        }

        // 2) Cache'te yoksa API'den çek
        var externalTasks = new[]
        {
            _externalApiService.FetchFootballMatchesAsync(),
            _externalApiService.FetchBasketballMatchesAsync(),
            _externalApiService.FetchVolleyballMatchesAsync()
        };

        var externalResults = await Task.WhenAll(externalTasks);
        var externalMatch = externalResults
            .SelectMany(list => list)
            .FirstOrDefault(m => m.Id == id);

        // 3) Geçmiş maçlar canlı listelerde yok — ID'den API-Sports tek maç çek
        externalMatch ??= await TryResolvePastMatchByExternalIdAsync(id);

        if (externalMatch == null)
            return NotFound();

        await TryLoadStatisticsAsync(externalMatch);
        return Ok(externalMatch);
    }

    /// <summary>
    /// "Football-12345" / "Basketball-..." formatında ID için fixture/game endpoint'i.
    /// </summary>
    private async Task<Match?> TryResolvePastMatchByExternalIdAsync(string id)
    {
        if (!ExternalApiService.TryParseSportAndExternalId(id, out var sport, out var extId))
            return null;

        return sport switch
        {
            SportType.Football   => await _externalApiService.FetchFootballMatchByFixtureIdAsync(extId),
            SportType.Basketball => await _externalApiService.FetchBasketballMatchByGameIdAsync(extId),
            SportType.Volleyball => await _externalApiService.FetchVolleyballMatchByGameIdAsync(extId),
            _ => null
        };
    }

    /// <summary>
    /// Maç tipine göre istatistikleri ve olayları API'den çekmeyi dener.
    /// ExternalFixtureId yoksa Match ID'den parse eder.
    /// </summary>
    private async Task TryLoadStatisticsAsync(Match match)
    {
        try
        {
            // ExternalFixtureId yoksa, Match ID'den parse et (Format: "SportType-{externalId}")
            var externalId = match.ExternalFixtureId 
                ?? ExternalApiService.ParseExternalIdFromMatchId(match.Id);

            if (!externalId.HasValue) return;

            // İstatistikleri çek
            Dictionary<string, object>? statistics = match.SportType switch
            {
                SportType.Football => await _externalApiService.FetchFootballMatchStatisticsAsync(externalId.Value),
                SportType.Basketball => await _externalApiService.FetchBasketballMatchStatisticsAsync(externalId.Value),
                SportType.Volleyball => await _externalApiService.FetchVolleyballMatchStatisticsAsync(externalId.Value),
                _ => null
            };

            match.Statistics = statistics;

            // Futbol maçları için olayları ve kadroları çek
            if (match.SportType == SportType.Football)
            {
                try
                {
                    var events = await _externalApiService.FetchFootballMatchEventsAsync(externalId.Value);
                    match.Events = events;
                }
                catch (Exception evEx)
                {
                    Console.WriteLine($"Olaylar çekilemedi: {evEx.Message}");
                }

                try
                {
                    match.Lineups = await _externalApiService.FetchFootballMatchLineupsAsync(
                        externalId.Value, match.HomeTeam, match.AwayTeam);
                }
                catch (Exception luEx)
                {
                    Console.WriteLine($"Kadrolar çekilemedi: {luEx.Message}");
                }

                if (string.IsNullOrWhiteSpace(match.StadiumName))
                {
                    try
                    {
                        var fromFixture = await _externalApiService.FetchFootballMatchByFixtureIdAsync(externalId.Value);
                        if (!string.IsNullOrWhiteSpace(fromFixture?.StadiumName))
                            match.StadiumName = fromFixture.StadiumName;
                    }
                    catch (Exception stEx)
                    {
                        Console.WriteLine($"Stadyum bilgisi çekilemedi: {stEx.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"İstatistik çekilemedi ({match.SportType}): {ex.Message}");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateMatch([FromBody] CreateMatchRequest request)
    {
        var match = new Match
        {
            HomeTeam = request.HomeTeam,
            AwayTeam = request.AwayTeam,
            League = request.League,
            StartTime = request.StartTime ?? DateTime.UtcNow,
            Status = MatchStatus.Live,
            SportType = request.SportType,
            CreatedAt = DateTime.UtcNow
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("MatchUpdated", match);

        return CreatedAtAction(nameof(GetMatch), new { id = match.Id }, match);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMatch(string id, [FromBody] UpdateMatchRequest request)
    {
        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == id);
        if (match == null)
            return NotFound();

        match.HomeScore = request.HomeScore;
        match.AwayScore = request.AwayScore;
        match.Minute = request.Minute;
        match.Status = request.Status;

        await _context.SaveChangesAsync();
        await _hubContext.Clients.All.SendAsync("MatchUpdated", match);

        return Ok(match);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMatch(string id)
    {
        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == id);
        if (match == null)
            return NotFound();

        _context.Matches.Remove(match);
        await _context.SaveChangesAsync();
        
        await _hubContext.Clients.All.SendAsync("MatchRemoved", id);

        return NoContent();
    }
}
