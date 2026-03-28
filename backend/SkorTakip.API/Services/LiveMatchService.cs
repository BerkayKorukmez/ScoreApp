using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Data;
using SkorTakip.API.Hubs;
using SkorTakip.API.Models;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Services;

/// <summary>
/// Her 2 dakikada bir tüm sporların maçlarını API'den çeker,
/// değişen maçları WebSocket üzerinden bağlı tüm istemcilere gönderir.
/// Admin tarafından gizlenen maçlar broadcast edilmez.
/// </summary>
public class LiveMatchService : BackgroundService, ILiveMatchService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<MatchHub> _hubContext;
    private readonly ILogger<LiveMatchService> _logger;

    // Son bilinen maç durumlarını saklar (Id → Match)
    private readonly Dictionary<string, Match> _matchCache = new();

    public LiveMatchService(
        IServiceProvider serviceProvider,
        IHubContext<MatchHub> hubContext,
        ILogger<LiveMatchService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LiveMatchService baslatildi.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAndBroadcastAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LiveMatchService beklenmeyen hata: {Message}", ex.Message);
            }

            // 2 dakikada bir güncelle (API rate limit: 10 istek/dakika)
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    private async Task FetchAndBroadcastAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var externalApi = scope.ServiceProvider.GetRequiredService<IExternalApiService>();

        var tasks = new[]
        {
            FetchSafely(() => externalApi.FetchFootballMatchesAsync(),   "Futbol"),
            FetchSafely(() => externalApi.FetchBasketballMatchesAsync(), "Basketbol"),
            FetchSafely(() => externalApi.FetchVolleyballMatchesAsync(), "Voleybol"),
        };

        var results = await Task.WhenAll(tasks);
        var allMatches = results.SelectMany(x => x).ToList();

        if (allMatches.Count == 0)
        {
            _logger.LogWarning("LiveMatchService: Hicbir spordan mac verisi alinamadi.");
            return;
        }

        _logger.LogInformation("LiveMatchService: Toplam {Count} mac API'den alindi.", allMatches.Count);

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hiddenIds = await db.Matches.Where(m => m.IsHidden).Select(m => m.Id).ToListAsync();
        var hiddenSet = hiddenIds.ToHashSet();

        var updatedCount = 0;

        foreach (var match in allMatches)
        {
            if (hiddenSet.Contains(match.Id))
                continue;

            if (_matchCache.TryGetValue(match.Id, out var cached))
            {
                if (cached.HomeScore != match.HomeScore ||
                    cached.AwayScore != match.AwayScore ||
                    cached.Status    != match.Status    ||
                    cached.Minute    != match.Minute)
                {
                    _matchCache[match.Id] = match;
                    await _hubContext.Clients.All.SendAsync("MatchUpdated", match, stoppingToken);
                    updatedCount++;
                }
            }
            else
            {
                _matchCache[match.Id] = match;
                await _hubContext.Clients.All.SendAsync("MatchUpdated", match, stoppingToken);
                updatedCount++;
            }
        }

        if (updatedCount > 0)
            _logger.LogInformation("LiveMatchService: {Count} mac WebSocket ile yayinlandi.", updatedCount);
    }

    private async Task<List<Match>> FetchSafely(Func<Task<List<Match>>> fetch, string sportName)
    {
        try
        {
            return await fetch();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("LiveMatchService: {Sport} maclari alinamadi: {Message}", sportName, ex.Message);
            return new List<Match>();
        }
    }

    /// <summary>
    /// Cache'teki tüm maçları döndürür.
    /// MatchHub.OnConnectedAsync ve MatchController tarafından kullanılır.
    /// </summary>
    public IEnumerable<Match> GetCachedMatches() => _matchCache.Values;
}
