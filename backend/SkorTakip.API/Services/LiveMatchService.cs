using Microsoft.AspNetCore.SignalR;
using SkorTakip.API.Hubs;
using SkorTakip.API.Interfaces;
using SkorTakip.API.Models;

namespace SkorTakip.API.Services;

/// <summary>
/// Her 30 saniyede bir tüm sporların maçlarını API'den çeker,
/// değişen maçları WebSocket üzerinden bağlı tüm istemcilere gönderir.
/// </summary>
public class LiveMatchService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<MatchHub> _hubContext;
    private readonly ILogger<LiveMatchService> _logger;

    // Son bilinen maç durumlarını saklar (Id → Match)
    private readonly Dictionary<string, Match> _matchCache = new();

    // Maç güncellemelerini isteyen tüm istemcilerin anlık listesini almak için
    public static readonly Dictionary<string, List<Match>> CurrentMatches = new();

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

        // Tüm sporları paralel çek (Tenis API planında mevcut değil, kaldırıldı)
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

        var updatedCount = 0;

        foreach (var match in allMatches)
        {
            if (_matchCache.TryGetValue(match.Id, out var cached))
            {
                // Değişiklik var mı kontrol et
                if (cached.HomeScore != match.HomeScore ||
                    cached.AwayScore != match.AwayScore ||
                    cached.Status    != match.Status    ||
                    cached.Minute    != match.Minute)
                {
                    // Değişiklik var → WebSocket ile yayınla
                    _matchCache[match.Id] = match;
                    await _hubContext.Clients.All.SendAsync("MatchUpdated", match);
                    updatedCount++;
                }
            }
            else
            {
                // Yeni maç → cache'e ekle ve yayınla
                _matchCache[match.Id] = match;
                await _hubContext.Clients.All.SendAsync("MatchUpdated", match);
                updatedCount++;
            }
        }

        // Güncelleme yoksa sadece log
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
    /// Yeni bağlanan bir istemciye cache'teki tüm maçları gönderir.
    /// MatchHub.OnConnectedAsync tarafından çağrılır.
    /// </summary>
    public IEnumerable<Match> GetCachedMatches() => _matchCache.Values;
}
