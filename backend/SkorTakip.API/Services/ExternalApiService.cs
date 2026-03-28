using SkorTakip.API.DTOs;
using SkorTakip.API.Models;
using SkorTakip.API.Services.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SkorTakip.API.Services;

/// <summary>
/// Harici API-Sports entegrasyonu.
/// Her spor için ayrı partial class dosyası bulunur:
///   ExternalApiService.Football.cs
///   ExternalApiService.Basketball.cs
///   ExternalApiService.Volleyball.cs
///   ExternalApiService.Tennis.cs
/// </summary>
public partial class ExternalApiService : IExternalApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiService> _logger;
    private readonly IConfiguration _configuration;

    // ========== STATIC CACHE (tüm instance'lar arasında paylaşılır) ==========
    private static readonly ConcurrentDictionary<string, (List<Match> Data, DateTime ExpiresAt)> _matchCache = new();
    private static readonly ConcurrentDictionary<int, (Dictionary<string, object>? Data, DateTime ExpiresAt)> _statsCache = new();
    private static readonly ConcurrentDictionary<string, (List<LeagueStandingDto> Data, DateTime ExpiresAt)> _standingsCache = new();
    private static readonly ConcurrentDictionary<string, (List<MatchResultDto>    Data, DateTime ExpiresAt)> _resultsCache   = new();

    // Cache süreleri
    private static readonly TimeSpan MatchCacheDuration     = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan StatsCacheDuration     = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan StandingsCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ResultsCacheDuration   = TimeSpan.FromMinutes(15);

    public ExternalApiService(HttpClient httpClient, ILogger<ExternalApiService> logger, IConfiguration configuration)
    {
        _httpClient    = httpClient;
        _logger        = logger;
        _configuration = configuration;
    }

    // -------------------------------------------------------------------------
    // Ortak yardımcılar
    // -------------------------------------------------------------------------

    private string GetApiKey(string sportType) => sportType switch
    {
        "Football"   => _configuration["ApiSports:FootballApiKey"]
                ?? throw new InvalidOperationException("ApiSports:FootballApiKey configuration is missing."),
            "Basketball" => _configuration["ApiSports:BasketballApiKey"] 
                ?? throw new InvalidOperationException("ApiSports:BasketballApiKey configuration is missing."),
            "Volleyball" => _configuration["ApiSports:VolleyballApiKey"] 
                ?? throw new InvalidOperationException("ApiSports:VolleyballApiKey configuration is missing."),
        "Tennis"     => _configuration["ApiSports:TennisApiKey"]
                ?? throw new InvalidOperationException("ApiSports:TennisApiKey configuration is missing."),
        _            => throw new InvalidOperationException($"Unknown sport type: {sportType}")
    };

    /// <summary>
    /// API-Sports istatistik dizisini anahtar-değer çiftlerine dönüştürür.
    /// </summary>
    private static Dictionary<string, object> ParseTeamStatistics(JsonElement teamElement)
    {
        var result = new Dictionary<string, object>();

        if (!teamElement.TryGetProperty("statistics", out var statsArray) ||
            statsArray.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var stat in statsArray.EnumerateArray())
        {
            var type         = stat.GetProperty("type").GetString() ?? string.Empty;
            var valueElement = stat.GetProperty("value");

            object value = valueElement.ValueKind switch
            {
                JsonValueKind.Number => valueElement.GetInt32(),
                JsonValueKind.String => valueElement.GetString() ?? "0",
                _                   => 0
            };

            result[type] = value;
        }

        return result;
    }

    /// <summary>
    /// Maç ID'sinden harici API game/fixture ID'sini parse eder.
    /// Format: "SportType-{externalId}" (ör: "Football-12345")
    /// </summary>
    public static int? ParseExternalIdFromMatchId(string matchId)
    {
        if (string.IsNullOrWhiteSpace(matchId)) return null;
        var lastDash = matchId.LastIndexOf('-');
        if (lastDash < 0 || lastDash >= matchId.Length - 1) return null;
        return int.TryParse(matchId[(lastDash + 1)..], out var id) ? id : null;
    }

    /// <summary>
    /// "Football-12345" formatındaki ID'den spor ve harici sayısal ID çıkarır (ilk '-' öncesi enum adı).
    /// </summary>
    public static bool TryParseSportAndExternalId(string matchId, out SportType sport, out int externalId)
    {
        sport = default;
        externalId = 0;
        if (string.IsNullOrWhiteSpace(matchId)) return false;
        var idx = matchId.IndexOf('-');
        if (idx <= 0 || idx >= matchId.Length - 1) return false;
        var prefix = matchId[..idx];
        var suffix = matchId[(idx + 1)..];
        if (!int.TryParse(suffix, out externalId)) return false;
        return Enum.TryParse(prefix, ignoreCase: true, out sport);
    }
}
