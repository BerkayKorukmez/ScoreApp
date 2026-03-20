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
                    SportType.Tennis => await _externalApiService.FetchTennisMatchesAsync(),
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
                .Where(m => m.SportType == sportType.Value && !m.IsHidden)
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

        var query = _context.Matches.Where(m => !m.IsHidden).AsQueryable();

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
    /// Belirtilen tarih ve spor tipine göre geçmiş maçları döner.
    /// Geçmiş tarihler için önce DB kontrol edilir; bulunursa API'ye istek atılmaz.
    /// Bulunmazsa dış API'den çekilip tamamlanmış maçlar DB'ye kaydedilir.
    /// Tarih formatı: yyyy-MM-dd (örn: 2026-03-01)
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetMatchHistory(
        [FromQuery] string date,
        [FromQuery] SportType sportType = SportType.Football)
    {
        if (string.IsNullOrWhiteSpace(date) || !DateTime.TryParse(date, out var parsedDate))
            return BadRequest("Geçerli bir tarih giriniz (yyyy-MM-dd).");

        var dayStart = parsedDate.Date;
        var dayEnd   = dayStart.AddDays(1);
        var isToday  = dayStart == DateTime.UtcNow.Date;

        // ── Geçmiş tarihler için önce DB'yi kontrol et (futbol hariç — CollectAPI) ─
        if (!isToday && sportType != SportType.Football)
        {
            var dbMatches = await _context.Matches
                .Where(m => m.SportType == sportType
                         && m.StartTime >= dayStart
                         && m.StartTime < dayEnd)
                .OrderByDescending(m => m.StartTime)
                .ToListAsync();

            if (dbMatches.Any())
            {
                Console.WriteLine($"Geçmiş maçlar DB'den döndürüldü: {dbMatches.Count} adet ({sportType}, {date})");
                return Ok(dbMatches);
            }
        }

        // ── API'den çek ──────────────────────────────────────────────────────────
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

        // ── Tamamlanmış maçları DB'ye kaydet (CollectAPI sentetik id'leri kaydetme) ─
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
                    .ToList();

                if (newMatches.Any())
                {
                    _context.Matches.AddRange(newMatches);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"DB'ye kaydedildi: {newMatches.Count} yeni biten maç ({sportType}, {date})");
                }
            }
        }

        matches = matches.OrderByDescending(m => m.StartTime).ToList();
        return Ok(matches);
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
            _externalApiService.FetchVolleyballMatchesAsync(),
            _externalApiService.FetchTennisMatchesAsync()
        };

        var externalResults = await Task.WhenAll(externalTasks);
        var externalMatch = externalResults
            .SelectMany(list => list)
            .FirstOrDefault(m => m.Id == id);

        if (externalMatch == null)
            return NotFound();

        await TryLoadStatisticsAsync(externalMatch);
        return Ok(externalMatch);
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

            // Futbol maçları için olayları da çek (goller, kartlar, değişiklikler)
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
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"İstatistik çekilemedi ({match.SportType}): {ex.Message}");
        }
    }

    [HttpPost]
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

    // ── CollectAPI: Son hafta maç sonuçları ──────────────────────────────────────
    // GET /api/match/results/football?collectApiKey=super-lig
    [HttpGet("results/football")]
    public async Task<IActionResult> GetFootballResults(
        [FromQuery] string? collectApiKey,
        [FromQuery] string? date)
    {
        if (string.IsNullOrWhiteSpace(collectApiKey))
            return BadRequest("collectApiKey parametresi zorunludur.");

        var results = await _externalApiService.FetchFootballResultsFromCollectApiAsync(collectApiKey, date);
        return Ok(results);
    }
}
