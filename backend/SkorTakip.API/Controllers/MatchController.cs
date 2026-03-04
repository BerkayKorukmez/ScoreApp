using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Data;
using SkorTakip.API.Hubs;
using SkorTakip.API.Interfaces;
using SkorTakip.API.Models;
using SkorTakip.API.Services;

namespace SkorTakip.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchController : ControllerBase
{
    private readonly IHubContext<MatchHub> _hubContext;
    private readonly ApplicationDbContext _context;
    private readonly IExternalApiService _externalApiService;
    private readonly LiveMatchService _liveMatchService;

    public MatchController(
        IHubContext<MatchHub> hubContext,
        ApplicationDbContext context,
        IExternalApiService externalApiService,
        LiveMatchService liveMatchService)
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
        // Eğer sportType verilmişse, önce ilgili spor için gerçek API'den verileri çek,
        // ardından aynı spor tipine ait veritabanı maçlarını da ekle.
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
                // External API hatası - sadece veritabanı maçlarını döndür
                // Log hatayı ama devam et
                Console.WriteLine($"External API hatası ({sportType.Value}): {ex.Message}");
            }

            var dbQuery = _context.Matches
                .Where(m => m.SportType == sportType.Value)
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

        // SportType yoksa, sadece veritabanındaki maçları döndür
        var query = _context.Matches.AsQueryable();

        if (!string.IsNullOrEmpty(league))
        {
            query = query.Where(m => m.League == league);
        }

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMatch(string id)
    {
        // Önce veritabanından dene
        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == id);
        if (match != null)
            return Ok(match);

        // 1) LiveMatchService cache'inden dene (API çağrısı yapmadan!)
        var cachedMatch = _liveMatchService.GetCachedMatches()
            .FirstOrDefault(m => m.Id == id);

        if (cachedMatch != null)
        {
            // Futbol maçı ise istatistikleri de çek (cache'li)
            if (cachedMatch.SportType == SportType.Football && cachedMatch.ExternalFixtureId.HasValue)
            {
                try
                {
                    var statistics = await _externalApiService.FetchFootballMatchStatisticsAsync(
                        cachedMatch.ExternalFixtureId.Value);
                    cachedMatch.Statistics = statistics;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"İstatistik çekilemedi: {ex.Message}");
                }
            }
            return Ok(cachedMatch);
        }

        // 2) Cache'te yoksa API'den çek (ExternalApiService kendi cache'ini kullanır)
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

        // Futbol maçı ise ve ExternalFixtureId varsa, istatistikleri de çek
        if (externalMatch.SportType == SportType.Football && externalMatch.ExternalFixtureId.HasValue)
        {
            try
            {
                var statistics = await _externalApiService.FetchFootballMatchStatisticsAsync(
                    externalMatch.ExternalFixtureId.Value);
                externalMatch.Statistics = statistics;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İstatistik çekilemedi: {ex.Message}");
            }
        }

        return Ok(externalMatch);
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
}

public class CreateMatchRequest
{
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public string League { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public SportType SportType { get; set; } = SportType.Football;
}

public class UpdateMatchRequest
{
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public int Minute { get; set; }
    public MatchStatus Status { get; set; }
}
