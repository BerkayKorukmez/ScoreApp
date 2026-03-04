using SkorTakip.API.Interfaces;
using SkorTakip.API.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SkorTakip.API.Services;

public class ExternalApiService : IExternalApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiService> _logger;
    private readonly IConfiguration _configuration;

    // ========== STATIC CACHE (tüm instance'lar arasında paylaşılır) ==========
    private static readonly ConcurrentDictionary<string, (List<Match> Data, DateTime ExpiresAt)> _matchCache = new();
    private static readonly ConcurrentDictionary<int, (Dictionary<string, object>? Data, DateTime ExpiresAt)> _statsCache = new();
    
    // Cache süreleri
    private static readonly TimeSpan MatchCacheDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan StatsCacheDuration = TimeSpan.FromMinutes(3);

    public ExternalApiService(HttpClient httpClient, ILogger<ExternalApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    private string GetApiKey(string sportType)
    {
        return sportType switch
        {
            "Football" => _configuration["ApiSports:FootballApiKey"] 
                ?? throw new InvalidOperationException("ApiSports:FootballApiKey configuration is missing."),
            "Basketball" => _configuration["ApiSports:BasketballApiKey"] 
                ?? throw new InvalidOperationException("ApiSports:BasketballApiKey configuration is missing."),
            "Volleyball" => _configuration["ApiSports:VolleyballApiKey"] 
                ?? throw new InvalidOperationException("ApiSports:VolleyballApiKey configuration is missing."),
            "Tennis" => _configuration["ApiSports:TennisApiKey"] 
                ?? throw new InvalidOperationException("ApiSports:TennisApiKey configuration is missing."),
            _ => throw new InvalidOperationException($"Unknown sport type: {sportType}")
        };
    }

    public async Task<List<Match>> FetchFootballMatchesAsync()
    {
        // Cache kontrolü
        if (_matchCache.TryGetValue("football", out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Futbol maclari CACHE'ten donduruluyor ({Count} mac)", cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Futbol maclari API-Sports uzerinden cekiliyor...");

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://v3.football.api-sports.io/fixtures?date={today}");

            request.Headers.Add("x-apisports-key", GetApiKey("Football"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Futbol API yaniti (ilk 300 karakter): {Content}", content.Length > 300 ? content[..300] : content);
            using var doc = JsonDocument.Parse(content);

            // API hata kontrolü
            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var err in errors.EnumerateObject())
                    _logger.LogError("Futbol API hatasi - {Key}: {Value}", err.Name, err.Value);
                return new List<Match>();
            }

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("API-Sports futbol yaniti beklenen formatta degil.");
                return new List<Match>();
            }

            _logger.LogInformation("Futbol: API'den {Count} mac alindi.", responseArray.GetArrayLength());
            var matches = new List<Match>();

            foreach (var item in responseArray.EnumerateArray())
            {
                try
                {
                    var fixture = item.GetProperty("fixture");
                    var league = item.GetProperty("league");
                    var teams = item.GetProperty("teams");
                    var goals = item.GetProperty("goals");

                    var homeTeam = teams.GetProperty("home").GetProperty("name").GetString() ?? string.Empty;
                    var awayTeam = teams.GetProperty("away").GetProperty("name").GetString() ?? string.Empty;

                    var homeGoals = goals.GetProperty("home").ValueKind == JsonValueKind.Number
                        ? goals.GetProperty("home").GetInt32()
                        : 0;
                    var awayGoals = goals.GetProperty("away").ValueKind == JsonValueKind.Number
                        ? goals.GetProperty("away").GetInt32()
                        : 0;

                    var leagueName = league.GetProperty("name").GetString() ?? string.Empty;

                    var dateString = fixture.GetProperty("date").GetString();
                    DateTime startTime = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(dateString) &&
                        DateTime.TryParse(dateString, out var parsedDate))
                    {
                        startTime = parsedDate;
                    }

                    var statusObj = fixture.GetProperty("status");
                    var shortStatus = statusObj.GetProperty("short").GetString() ?? string.Empty;
                    var elapsed = statusObj.TryGetProperty("elapsed", out var elapsedElement) &&
                                  elapsedElement.ValueKind == JsonValueKind.Number
                        ? elapsedElement.GetInt32()
                        : 0;

                    var status = shortStatus switch
                    {
                        "FT" => MatchStatus.Finished,
                        "NS" => MatchStatus.NotStarted,
                        "HT" => MatchStatus.HalfTime,
                        _ => MatchStatus.Live
                    };

                    // API-Sports fixture ID'sini al
                    var fixtureId = fixture.GetProperty("id").GetInt32();

                    // Aynı maçı her istekte tekrar bulabilmek için deterministik bir ID üret
                    var generatedId = $"{SportType.Football}-{fixtureId}";

                    var match = new Match
                    {
                        Id = generatedId,
                        HomeTeam = homeTeam,
                        AwayTeam = awayTeam,
                        HomeScore = homeGoals,
                        AwayScore = awayGoals,
                        League = leagueName,
                        StartTime = startTime,
                        Minute = elapsed,
                        Status = status,
                        SportType = SportType.Football,
                        ExternalFixtureId = fixtureId
                    };

                    matches.Add(match);
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "Futbol maci parse edilirken hata olustu, atlaniyor.");
                }
            }

            // Cache'e kaydet
            _matchCache["football"] = (matches, DateTime.UtcNow.Add(MatchCacheDuration));
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Futbol maclari cekilirken hata olustu");
            // Cache'te eski veri varsa onu döndür
            if (_matchCache.TryGetValue("football", out var stale))
                return stale.Data;
            return new List<Match>();
        }
    }

    public async Task<List<Match>> FetchBasketballMatchesAsync()
    {
        // Cache kontrolü
        if (_matchCache.TryGetValue("basketball", out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Basketbol maclari CACHE'ten donduruluyor ({Count} mac)", cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Basketbol maclari API-Sports uzerinden cekiliyor...");

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://v1.basketball.api-sports.io/games?date={today}");

            request.Headers.Add("x-apisports-key", GetApiKey("Basketball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Basketbol API yaniti (ilk 300 karakter): {Content}", content.Length > 300 ? content[..300] : content);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                // errors bazen array, bazen object olabiliyor
                if (errors.ValueKind == JsonValueKind.Object && errors.EnumerateObject().Any())
                {
                    foreach (var err in errors.EnumerateObject())
                        _logger.LogError("Basketbol API hatasi - {Key}: {Value}", err.Name, err.Value);
                    return new List<Match>();
                }
                if (errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
                {
                    _logger.LogError("Basketbol API hatasi: {Errors}", errors.ToString());
                    return new List<Match>();
                }
            }

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("API-Sports basketbol yaniti beklenen formatta degil.");
                return new List<Match>();
            }

            _logger.LogInformation("Basketbol: API'den {Count} mac alindi.", responseArray.GetArrayLength());
            var matches = new List<Match>();

            foreach (var item in responseArray.EnumerateArray())
            {
                try
                {
                    // Basketbol API: id, date, status, league, teams, scores doğrudan item içinde
                    var league = item.GetProperty("league");
                    var teams = item.GetProperty("teams");
                    var scores = item.GetProperty("scores");

                    var homeTeam = teams.GetProperty("home").GetProperty("name").GetString() ?? string.Empty;
                    var awayTeam = teams.GetProperty("away").GetProperty("name").GetString() ?? string.Empty;

                    // Skor: scores.home.total ve scores.away.total
                    var homeScoreElement = scores.GetProperty("home").GetProperty("total");
                    var awayScoreElement = scores.GetProperty("away").GetProperty("total");
                    var homeScore = homeScoreElement.ValueKind == JsonValueKind.Number ? homeScoreElement.GetInt32() : 0;
                    var awayScore = awayScoreElement.ValueKind == JsonValueKind.Number ? awayScoreElement.GetInt32() : 0;

                    var leagueName = league.GetProperty("name").GetString() ?? string.Empty;

                    // Tarih doğrudan item içinde
                    var dateString = item.GetProperty("date").GetString();
                    DateTime startTime = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(dateString) &&
                        DateTime.TryParse(dateString, out var parsedDate))
                    {
                        startTime = parsedDate;
                    }

                    // Durum doğrudan item içinde
                    var statusObj = item.GetProperty("status");
                    var shortStatus = statusObj.GetProperty("short").GetString() ?? string.Empty;

                    var status = shortStatus switch
                    {
                        "FT" or "AOT" => MatchStatus.Finished,
                        "NS" => MatchStatus.NotStarted,
                        "HT" or "BT" => MatchStatus.HalfTime,
                        _ => MatchStatus.Live
                    };

                    // ID doğrudan item içinde
                    var gameId = item.GetProperty("id").GetInt32();
                    var generatedId = $"{SportType.Basketball}-{gameId}";

                    var match = new Match
                    {
                        Id = generatedId,
                        HomeTeam = homeTeam,
                        AwayTeam = awayTeam,
                        HomeScore = homeScore,
                        AwayScore = awayScore,
                        League = leagueName,
                        StartTime = startTime,
                        Status = status,
                        SportType = SportType.Basketball
                    };

                    matches.Add(match);
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "Basketbol maci parse edilirken hata olustu, atlaniyor.");
                }
            }

            _matchCache["basketball"] = (matches, DateTime.UtcNow.Add(MatchCacheDuration));
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Basketbol maclari cekilirken hata olustu");
            if (_matchCache.TryGetValue("basketball", out var stale))
                return stale.Data;
            return new List<Match>();
        }
    }

    public async Task<List<Match>> FetchVolleyballMatchesAsync()
    {
        // Cache kontrolü
        if (_matchCache.TryGetValue("volleyball", out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Voleybol maclari CACHE'ten donduruluyor ({Count} mac)", cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Voleybol maclari API-Sports uzerinden cekiliyor...");

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://v1.volleyball.api-sports.io/games?date={today}");

            request.Headers.Add("x-apisports-key", GetApiKey("Volleyball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Voleybol API yaniti (ilk 300 karakter): {Content}", content.Length > 300 ? content[..300] : content);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                if (errors.ValueKind == JsonValueKind.Object && errors.EnumerateObject().Any())
                {
                    foreach (var err in errors.EnumerateObject())
                        _logger.LogError("Voleybol API hatasi - {Key}: {Value}", err.Name, err.Value);
                    return new List<Match>();
                }
                if (errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
                {
                    _logger.LogError("Voleybol API hatasi: {Errors}", errors.ToString());
                    return new List<Match>();
                }
            }

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("API-Sports voleybol yaniti beklenen formatta degil.");
                return new List<Match>();
            }

            _logger.LogInformation("Voleybol: API'den {Count} mac alindi.", responseArray.GetArrayLength());
            var matches = new List<Match>();

            foreach (var item in responseArray.EnumerateArray())
            {
                try
                {
                    // Voleybol API: id, date, status, league, teams, scores doğrudan item içinde
                    var league = item.GetProperty("league");
                    var teams = item.GetProperty("teams");

                    var homeTeam = teams.GetProperty("home").GetProperty("name").GetString() ?? string.Empty;
                    var awayTeam = teams.GetProperty("away").GetProperty("name").GetString() ?? string.Empty;

                    var leagueName = league.GetProperty("name").GetString() ?? string.Empty;

                    // Skor: scores.home ve scores.away (set skorları, doğrudan integer)
                    int homeScore = 0;
                    int awayScore = 0;
                    if (item.TryGetProperty("scores", out var scores))
                    {
                        var homeScoreEl = scores.GetProperty("home");
                        var awayScoreEl = scores.GetProperty("away");
                        homeScore = homeScoreEl.ValueKind == JsonValueKind.Number ? homeScoreEl.GetInt32() : 0;
                        awayScore = awayScoreEl.ValueKind == JsonValueKind.Number ? awayScoreEl.GetInt32() : 0;
                    }

                    // Tarih doğrudan item içinde
                    var dateString = item.GetProperty("date").GetString();
                    DateTime startTime = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(dateString) &&
                        DateTime.TryParse(dateString, out var parsedDate))
                    {
                        startTime = parsedDate;
                    }

                    // Durum doğrudan item içinde
                    var statusObj = item.GetProperty("status");
                    var shortStatus = statusObj.GetProperty("short").GetString() ?? string.Empty;

                    var status = shortStatus switch
                    {
                        "FT" => MatchStatus.Finished,
                        "NS" => MatchStatus.NotStarted,
                        "BT" or "SET1" or "SET2" or "SET3" or "SET4" or "SET5" => MatchStatus.Live,
                        _ => MatchStatus.Live
                    };

                    // NS (Not Started) durumunda canlı gibi gösterme
                    if (shortStatus == "NS") status = MatchStatus.NotStarted;

                    // ID doğrudan item içinde
                    var vGameId = item.GetProperty("id").GetInt32();
                    var generatedId = $"{SportType.Volleyball}-{vGameId}";

                    var match = new Match
                    {
                        Id = generatedId,
                        HomeTeam = homeTeam,
                        AwayTeam = awayTeam,
                        HomeScore = homeScore,
                        AwayScore = awayScore,
                        League = leagueName,
                        StartTime = startTime,
                        Status = status,
                        SportType = SportType.Volleyball
                    };

                    matches.Add(match);
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "Voleybol maci parse edilirken hata olustu, atlaniyor.");
                }
            }

            _matchCache["volleyball"] = (matches, DateTime.UtcNow.Add(MatchCacheDuration));
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voleybol maclari cekilirken hata olustu");
            if (_matchCache.TryGetValue("volleyball", out var stale))
                return stale.Data;
            return new List<Match>();
        }
    }

    public async Task<List<Match>> FetchTennisMatchesAsync()
    {
        // Cache kontrolü
        if (_matchCache.TryGetValue("tennis", out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Tenis maclari CACHE'ten donduruluyor ({Count} mac)", cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Tenis maclari API-Sports uzerinden cekiliyor...");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://v1.tennis.api-sports.io/matches?live=all");

            request.Headers.Add("x-apisports-key", GetApiKey("Tennis"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("API-Sports tenis yaniti beklenen formatta degil.");
                return new List<Match>();
            }

            var matches = new List<Match>();

            foreach (var item in responseArray.EnumerateArray())
            {
                try
                {
                    var matchInfo = item.GetProperty("match");
                    var league = item.GetProperty("league");
                    var teams = item.GetProperty("teams");

                    var homeTeam = teams.GetProperty("home").GetProperty("name").GetString() ?? string.Empty;
                    var awayTeam = teams.GetProperty("away").GetProperty("name").GetString() ?? string.Empty;

                    var leagueName = league.GetProperty("name").GetString() ?? string.Empty;

                    var dateString = matchInfo.GetProperty("date").GetString();
                    DateTime startTime = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(dateString) &&
                        DateTime.TryParse(dateString, out var parsedDate))
                    {
                        startTime = parsedDate;
                    }

                    var statusObj = matchInfo.GetProperty("status");
                    var shortStatus = statusObj.GetProperty("short").GetString() ?? string.Empty;

                    var status = shortStatus switch
                    {
                        "FT" => MatchStatus.Finished,
                        "NS" => MatchStatus.NotStarted,
                        _ => MatchStatus.Live
                    };

                    var tMatchId = matchInfo.GetProperty("id").GetInt32();
                    var generatedId = $"{SportType.Tennis}-{tMatchId}";

                    var match = new Match
                    {
                        Id = generatedId,
                        HomeTeam = homeTeam,
                        AwayTeam = awayTeam,
                        League = leagueName,
                        StartTime = startTime,
                        Status = status,
                        SportType = SportType.Tennis
                    };

                    matches.Add(match);
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "Tenis maci parse edilirken hata olustu, atlaniyor.");
                }
            }

            _matchCache["tennis"] = (matches, DateTime.UtcNow.Add(MatchCacheDuration));
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenis maclari cekilirken hata olustu");
            if (_matchCache.TryGetValue("tennis", out var stale))
                return stale.Data;
            return new List<Match>();
        }
    }

    /// <summary>
    /// API-Sports'tan futbol maçı istatistiklerini çeker (cache'li)
    /// </summary>
    public async Task<Dictionary<string, object>?> FetchFootballMatchStatisticsAsync(int fixtureId)
    {
        // Cache kontrolü
        if (_statsCache.TryGetValue(fixtureId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Istatistikler CACHE'ten donduruluyor - Fixture ID: {FixtureId}", fixtureId);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Futbol mac istatistikleri cekiliyor - Fixture ID: {FixtureId}", fixtureId);

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://v3.football.api-sports.io/fixtures/statistics?fixture={fixtureId}");

            request.Headers.Add("x-apisports-key", GetApiKey("Football"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array ||
                responseArray.GetArrayLength() < 2)
            {
                _logger.LogWarning("Istatistik verisi bulunamadi - Fixture ID: {FixtureId}", fixtureId);
                return null;
            }

            // İlk takım (ev sahibi) ve ikinci takım (deplasman) istatistikleri
            var homeStats = ParseTeamStatistics(responseArray[0]);
            var awayStats = ParseTeamStatistics(responseArray[1]);

            var stats = new Dictionary<string, object>
            {
                { "homeShotsTotal", homeStats.GetValueOrDefault("Total Shots", 0) },
                { "awayShotsTotal", awayStats.GetValueOrDefault("Total Shots", 0) },
                { "homeShotsOnGoal", homeStats.GetValueOrDefault("Shots on Goal", 0) },
                { "awayShotsOnGoal", awayStats.GetValueOrDefault("Shots on Goal", 0) },
                { "homePossession", homeStats.GetValueOrDefault("Ball Possession", "0%") },
                { "awayPossession", awayStats.GetValueOrDefault("Ball Possession", "0%") },
                { "homeCorners", homeStats.GetValueOrDefault("Corner Kicks", 0) },
                { "awayCorners", awayStats.GetValueOrDefault("Corner Kicks", 0) },
                { "homeFouls", homeStats.GetValueOrDefault("Fouls", 0) },
                { "awayFouls", awayStats.GetValueOrDefault("Fouls", 0) },
                { "homeYellowCards", homeStats.GetValueOrDefault("Yellow Cards", 0) },
                { "awayYellowCards", awayStats.GetValueOrDefault("Yellow Cards", 0) },
                { "homeRedCards", homeStats.GetValueOrDefault("Red Cards", 0) },
                { "awayRedCards", awayStats.GetValueOrDefault("Red Cards", 0) },
                { "homeOffsides", homeStats.GetValueOrDefault("Offsides", 0) },
                { "awayOffsides", awayStats.GetValueOrDefault("Offsides", 0) },
                { "homeSaves", homeStats.GetValueOrDefault("Goalkeeper Saves", 0) },
                { "awaySaves", awayStats.GetValueOrDefault("Goalkeeper Saves", 0) },
                { "homeTotalPasses", homeStats.GetValueOrDefault("Total passes", 0) },
                { "awayTotalPasses", awayStats.GetValueOrDefault("Total passes", 0) },
                { "homePassAccuracy", homeStats.GetValueOrDefault("Passes %", "0%") },
                { "awayPassAccuracy", awayStats.GetValueOrDefault("Passes %", "0%") },
                { "homeBlockedShots", homeStats.GetValueOrDefault("Blocked Shots", 0) },
                { "awayBlockedShots", awayStats.GetValueOrDefault("Blocked Shots", 0) }
            };

            // Cache'e kaydet
            _statsCache[fixtureId] = (stats, DateTime.UtcNow.Add(StatsCacheDuration));
            _logger.LogInformation("Istatistikler basariyla cekildi - Fixture ID: {FixtureId}", fixtureId);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Istatistikler cekilirken hata olustu - Fixture ID: {FixtureId}", fixtureId);
            // Cache'te eski veri varsa döndür
            if (_statsCache.TryGetValue(fixtureId, out var stale))
                return stale.Data;
            return null;
        }
    }

    /// <summary>
    /// API-Sports istatistik dizisini anahtar-değer çiftlerine dönüştürür
    /// </summary>
    private Dictionary<string, object> ParseTeamStatistics(JsonElement teamElement)
    {
        var result = new Dictionary<string, object>();

        if (!teamElement.TryGetProperty("statistics", out var statsArray) ||
            statsArray.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var stat in statsArray.EnumerateArray())
        {
            var type = stat.GetProperty("type").GetString() ?? string.Empty;
            var valueElement = stat.GetProperty("value");

            object value = valueElement.ValueKind switch
            {
                JsonValueKind.Number => valueElement.GetInt32(),
                JsonValueKind.String => valueElement.GetString() ?? "0",
                JsonValueKind.Null => 0,
                _ => 0
            };

            result[type] = value;
        }

        return result;
    }
}
