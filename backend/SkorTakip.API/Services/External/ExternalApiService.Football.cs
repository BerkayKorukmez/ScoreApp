using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using SkorTakip.API.Constants;
using SkorTakip.API.DTOs;
using SkorTakip.API.Models;
using System.Text.Json;

namespace SkorTakip.API.Services;

public partial class ExternalApiService
{
    // =========================================================================
    // FUTBOL — Günlük maçlar
    // =========================================================================

    public async Task<List<Match>> FetchFootballMatchesAsync()
    {
        const string cacheKey = "football";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Futbol maclari CACHE'ten donduruluyor ({Count} mac)", cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Futbol maclari API-Sports uzerinden cekiliyor...");

            var today   = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/fixtures?date={today}");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Futbol API yaniti (ilk 300 karakter): {Content}",
                content.Length > 300 ? content[..300] : content);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var err in errors.EnumerateObject())
                    _logger.LogError("Futbol API hatasi - {Key}: {Value}", err.Name, err.Value);
                return [];
            }

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("API-Sports futbol yaniti beklenen formatta degil.");
                return [];
            }

            _logger.LogInformation("Futbol: API'den {Count} mac alindi.", responseArray.GetArrayLength());
            var matches = ParseFootballFixtures(responseArray);

            _matchCache[cacheKey] = (matches, DateTime.UtcNow.Add(MatchCacheDuration));
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Futbol maclari cekilirken hata olustu");
            return _matchCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }

    // =========================================================================
    // FUTBOL — Tarih bazlı geçmiş maçlar (API-Sports — tüm ligler)
    // =========================================================================

    public async Task<List<Match>> FetchFootballMatchesByDateAsync(string date)
    {
        var cacheKey = $"football_history_{date}";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Futbol gecmis CACHE. Tarih: {Date} ({Count} mac)", date, cached.Data.Count);
            return cached.Data;
        }

        if (!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var requestedDay))
        {
            _logger.LogWarning("Gecersiz tarih: {Date}", date);
            return [];
        }

        var requestedYyyyMmDd = requestedDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        try
        {
            _logger.LogInformation("Futbol gecmis maclari API-Sports'tan cekiliyor. Tarih: {Date}", requestedYyyyMmDd);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/fixtures?date={requestedYyyyMmDd}");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var err in errors.EnumerateObject())
                    _logger.LogError("Futbol gecmis API hatasi - {Key}: {Value}", err.Name, err.Value);
                return [];
            }

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Futbol gecmis API yaniti beklenen formatta degil.");
                return [];
            }

            var matches = ParseFootballFixtures(responseArray);
            var ordered = matches.OrderByDescending(m => m.StartTime).ToList();

            _logger.LogInformation("Futbol gecmis: {Count} mac. Tarih: {Date}", ordered.Count, requestedYyyyMmDd);

            var historyTtl = ordered.Count == 0 ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(30);
            _matchCache[cacheKey] = (ordered, DateTime.UtcNow.Add(historyTtl));
            return ordered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Futbol gecmis API hatasi. Tarih: {Date}", date);
            return _matchCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }

    /// <summary>
    /// API-Sports fixture ID ile tek maç (geçmiş maç detayı / istatistik için).
    /// </summary>
    public async Task<Match?> FetchFootballMatchByFixtureIdAsync(int fixtureId)
    {
        try
        {
            _logger.LogInformation("Futbol tek fixture cekiliyor. Id: {Id}", fixtureId);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/fixtures?id={fixtureId}");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var err in errors.EnumerateObject())
                    _logger.LogError("Futbol fixture id API hatasi - {Key}: {Value}", err.Name, err.Value);
                return null;
            }

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array ||
                responseArray.GetArrayLength() < 1)
            {
                _logger.LogWarning("Futbol fixture bulunamadi. Id: {Id}", fixtureId);
                return null;
            }

            var matches = ParseFootballFixtures(responseArray);
            return matches.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Futbol tek fixture cekilemedi. Id: {Id}", fixtureId);
            return null;
        }
    }

    private static bool MatchesCollectResultDate(string? isoDate, string requestedYyyyMmDd)
    {
        if (string.IsNullOrWhiteSpace(isoDate))
            return false;
        // CollectAPI: "2026-03-19T17:00:00+03:00" — offset korunmalı (UTC'ye çevirip gün kaydırma)
        if (!DateTimeOffset.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto)
            && !DateTimeOffset.TryParse(isoDate, out dto))
            return false;

        return string.Equals(dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), requestedYyyyMmDd, StringComparison.Ordinal);
    }

    private static Match MapCollectResultToMatch(MatchResultDto r, CollectFootballLeagues.Entry league, string dateFallback)
    {
        var startTime = DateTime.TryParse(r.Date, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var st)
            ? st
            : DateTime.Parse(dateFallback, CultureInfo.InvariantCulture, DateTimeStyles.None);

        var id = BuildCollectHistoryMatchId(dateFallback, league.CollectKey, r.HomeTeam, r.AwayTeam, r.Date);

        return new Match
        {
            Id             = id,
            HomeTeam       = r.HomeTeam,
            AwayTeam       = r.AwayTeam,
            HomeScore      = r.HomeScore ?? 0,
            AwayScore      = r.AwayScore ?? 0,
            Minute         = 0,
            League         = league.LeagueName,
            LeagueCountry  = league.Country,
            ExternalLeagueId = league.ExternalLeagueId,
            LeagueFlag     = league.LeagueFlag,
            StartTime      = startTime,
            Status         = r.IsPlayed ? MatchStatus.Finished : MatchStatus.NotStarted,
            SportType      = SportType.Football,
            CreatedAt      = DateTime.UtcNow
        };
    }

    private static string BuildCollectHistoryMatchId(string date, string leagueKey, string home, string away, string? isoDate)
    {
        var raw = $"{date}|{leagueKey}|{home}|{away}|{isoDate ?? ""}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return $"c-{hash[..32]}";
    }

    // =========================================================================
    // FUTBOL — Maç istatistikleri
    // =========================================================================

    public async Task<Dictionary<string, object>?> FetchFootballMatchStatisticsAsync(int fixtureId)
    {
        if (_statsCache.TryGetValue(fixtureId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Istatistikler CACHE'ten donduruluyor - Fixture ID: {FixtureId}", fixtureId);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Futbol mac istatistikleri cekiliyor - Fixture ID: {FixtureId}", fixtureId);

            var request = new HttpRequestMessage(HttpMethod.Get,
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

            var homeStats = ParseTeamStatistics(responseArray[0]);
            var awayStats = ParseTeamStatistics(responseArray[1]);

            var stats = new Dictionary<string, object>
            {
                { "sportType",        "football" },
                { "homeShotsTotal",   homeStats.GetValueOrDefault("Total Shots", 0) },
                { "awayShotsTotal",   awayStats.GetValueOrDefault("Total Shots", 0) },
                { "homeShotsOnGoal",  homeStats.GetValueOrDefault("Shots on Goal", 0) },
                { "awayShotsOnGoal",  awayStats.GetValueOrDefault("Shots on Goal", 0) },
                { "homePossession",   homeStats.GetValueOrDefault("Ball Possession", "0%") },
                { "awayPossession",   awayStats.GetValueOrDefault("Ball Possession", "0%") },
                { "homeCorners",      homeStats.GetValueOrDefault("Corner Kicks", 0) },
                { "awayCorners",      awayStats.GetValueOrDefault("Corner Kicks", 0) },
                { "homeFouls",        homeStats.GetValueOrDefault("Fouls", 0) },
                { "awayFouls",        awayStats.GetValueOrDefault("Fouls", 0) },
                { "homeYellowCards",  homeStats.GetValueOrDefault("Yellow Cards", 0) },
                { "awayYellowCards",  awayStats.GetValueOrDefault("Yellow Cards", 0) },
                { "homeRedCards",     homeStats.GetValueOrDefault("Red Cards", 0) },
                { "awayRedCards",     awayStats.GetValueOrDefault("Red Cards", 0) },
                { "homeOffsides",     homeStats.GetValueOrDefault("Offsides", 0) },
                { "awayOffsides",     awayStats.GetValueOrDefault("Offsides", 0) },
                { "homeSaves",        homeStats.GetValueOrDefault("Goalkeeper Saves", 0) },
                { "awaySaves",        awayStats.GetValueOrDefault("Goalkeeper Saves", 0) },
                { "homeTotalPasses",  homeStats.GetValueOrDefault("Total passes", 0) },
                { "awayTotalPasses",  awayStats.GetValueOrDefault("Total passes", 0) },
                { "homePassAccuracy", homeStats.GetValueOrDefault("Passes %", "0%") },
                { "awayPassAccuracy", awayStats.GetValueOrDefault("Passes %", "0%") },
                { "homeBlockedShots", homeStats.GetValueOrDefault("Blocked Shots", 0) },
                { "awayBlockedShots", awayStats.GetValueOrDefault("Blocked Shots", 0) }
            };

            _statsCache[fixtureId] = (stats, DateTime.UtcNow.Add(StatsCacheDuration));
            _logger.LogInformation("Istatistikler basariyla cekildi - Fixture ID: {FixtureId}", fixtureId);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Istatistikler cekilirken hata olustu - Fixture ID: {FixtureId}", fixtureId);
            return _statsCache.TryGetValue(fixtureId, out var stale) ? stale.Data : null;
        }
    }

    // =========================================================================
    // FUTBOL — Maç olayları (goller, kartlar, değişiklikler)
    // =========================================================================

    public async Task<List<Dictionary<string, object>>?> FetchFootballMatchEventsAsync(int fixtureId)
    {
        var eventsCacheKey = -(fixtureId + 5000);
        if (_statsCache.TryGetValue(eventsCacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow &&
            cached.Data?.ContainsKey("_events") == true &&
            cached.Data["_events"] is List<Dictionary<string, object>> cachedEvents)
        {
            _logger.LogInformation("Olaylar CACHE'ten donduruluyor - Fixture ID: {FixtureId}", fixtureId);
            return cachedEvents;
        }

        try
        {
            _logger.LogInformation("Futbol mac olaylari cekiliyor - Fixture ID: {FixtureId}", fixtureId);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/fixtures/events?fixture={fixtureId}");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array ||
                responseArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("Olay verisi bulunamadi - Fixture ID: {FixtureId}", fixtureId);
                return null;
            }

            var events = new List<Dictionary<string, object>>();

            foreach (var ev in responseArray.EnumerateArray())
            {
                try
                {
                    var eventData = new Dictionary<string, object>();

                    if (ev.TryGetProperty("time", out var time))
                    {
                        var elapsed = time.TryGetProperty("elapsed", out var el) && el.ValueKind == JsonValueKind.Number
                            ? el.GetInt32() : 0;
                        var extra = time.TryGetProperty("extra", out var ext) && ext.ValueKind == JsonValueKind.Number
                            ? ext.GetInt32() : (int?)null;
                        eventData["minute"] = elapsed;
                        if (extra.HasValue) eventData["extraMinute"] = extra.Value;
                    }

                    if (ev.TryGetProperty("team", out var team))
                    {
                        eventData["teamName"] = team.GetProperty("name").GetString() ?? "";
                        if (team.TryGetProperty("logo", out var logo))
                            eventData["teamLogo"] = logo.GetString() ?? "";
                    }

                    if (ev.TryGetProperty("player", out var player))
                        eventData["playerName"] = player.GetProperty("name").GetString() ?? "";

                    if (ev.TryGetProperty("assist", out var assist))
                    {
                        var assistName = assist.GetProperty("name").GetString();
                        if (!string.IsNullOrEmpty(assistName))
                            eventData["assistName"] = assistName;
                    }

                    eventData["type"]   = ev.GetProperty("type").GetString()   ?? "";
                    eventData["detail"] = ev.GetProperty("detail").GetString() ?? "";

                    events.Add(eventData);
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "Futbol olayi parse edilirken hata, atlaniyor.");
                }
            }

            var cacheWrapper = new Dictionary<string, object> { { "_events", events } };
            _statsCache[eventsCacheKey] = (cacheWrapper, DateTime.UtcNow.Add(StatsCacheDuration));
            _logger.LogInformation("Futbol olaylari basariyla cekildi - {Count} olay - Fixture ID: {FixtureId}", events.Count, fixtureId);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Futbol olaylari cekilirken hata olustu - Fixture ID: {FixtureId}", fixtureId);
            return null;
        }
    }

    // =========================================================================
    // FUTBOL — Puan durumu (CollectAPI)
    // =========================================================================

    public async Task<List<LeagueStandingDto>> FetchFootballStandingsFromCollectApiAsync(string leagueKey)
    {
        var cacheKey = $"collectapi_standings_{leagueKey}";
        if (_standingsCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("CollectAPI puan durumu CACHE'ten donduruluyor. Lig: {LeagueKey} ({Count} takim)",
                leagueKey, cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("CollectAPI futbol puan durumu cekiliyor. Lig: {LeagueKey}", leagueKey);

            var collectApiKey = _configuration["CollectApi:ApiKey"]
                ?? throw new InvalidOperationException("CollectApi:ApiKey configuration is missing.");

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.collectapi.com/football/league?league={Uri.EscapeDataString(leagueKey)}");
            request.Headers.Add("authorization", $"apikey {collectApiKey}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("CollectAPI yaniti (ilk 300 karakter): {Content}",
                content.Length > 300 ? content[..300] : content);

            using var doc = JsonDocument.Parse(content);

            // CollectAPI doğrudan array döner
            JsonElement standingsArray;
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                standingsArray = doc.RootElement;
            }
            else if (doc.RootElement.TryGetProperty("success", out var successEl) &&
                     successEl.GetBoolean() == false)
            {
                var msg = doc.RootElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Bilinmeyen hata";
                _logger.LogWarning("CollectAPI hatasi: {Message}. Lig: {LeagueKey}", msg, leagueKey);
                return [];
            }
            else
            {
                _logger.LogWarning("CollectAPI beklenmeyen yanit formati. Lig: {LeagueKey}", leagueKey);
                return [];
            }

            var result = new List<LeagueStandingDto>();
            var rank   = 0;

            foreach (var item in standingsArray.EnumerateArray())
            {
                try
                {
                    rank++;
                    var teamName     = item.TryGetProperty("team", out var teamEl)      ? teamEl.GetString()  ?? string.Empty : string.Empty;
                    var played       = item.TryGetProperty("play", out var playEl)      && playEl.ValueKind == JsonValueKind.Number ? playEl.GetInt32() : 0;
                    var won          = item.TryGetProperty("win", out var winEl)        && winEl.ValueKind  == JsonValueKind.Number ? winEl.GetInt32()  : 0;
                    var drawn        = item.TryGetProperty("draw", out var drawEl)      && drawEl.ValueKind == JsonValueKind.Number ? drawEl.GetInt32() : 0;
                    var lost         = item.TryGetProperty("lose", out var loseEl)      && loseEl.ValueKind == JsonValueKind.Number ? loseEl.GetInt32() : 0;
                    var goalsFor     = item.TryGetProperty("goalfor", out var gfEl)     && gfEl.ValueKind   == JsonValueKind.Number ? gfEl.GetInt32()   : 0;
                    var goalsAgainst = item.TryGetProperty("goalagainst", out var gaEl) && gaEl.ValueKind   == JsonValueKind.Number ? gaEl.GetInt32()   : 0;
                    var points       = item.TryGetProperty("point", out var ptEl)       && ptEl.ValueKind   == JsonValueKind.Number ? ptEl.GetInt32()   : 0;
                    var apiRank      = item.TryGetProperty("rank", out var rkEl)        && rkEl.ValueKind   == JsonValueKind.Number ? rkEl.GetInt32()   : rank;

                    result.Add(new LeagueStandingDto
                    {
                        Rank           = apiRank,
                        TeamName       = teamName,
                        TeamLogo       = null,
                        Played         = played,
                        Won            = won,
                        Drawn          = drawn,
                        Lost           = lost,
                        GoalsFor       = goalsFor,
                        GoalsAgainst   = goalsAgainst,
                        GoalDifference = goalsFor - goalsAgainst,
                        Points         = points
                    });
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "CollectAPI puan durumu elemani parse edilirken hata, atlaniyor.");
                }
            }

            var sorted = result.OrderBy(s => s.Rank).ToList();
            _standingsCache[cacheKey] = (sorted, DateTime.UtcNow.Add(StandingsCacheDuration));
            _logger.LogInformation("CollectAPI puan durumu basariyla cekildi. Lig: {LeagueKey}, {Count} takim",
                leagueKey, sorted.Count);
            return sorted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CollectAPI puan durumu cekilirken hata. Lig: {LeagueKey}", leagueKey);
            return _standingsCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }

    // =========================================================================
    // FUTBOL — Puan durumu (API-Sports)
    // =========================================================================

    public async Task<List<LeagueStandingDto>> FetchFootballStandingsAsync(int leagueId, int season)
    {
        var cacheKey = $"standings_{leagueId}_{season}";
        if (_standingsCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Puan durumu CACHE'ten donduruluyor. Lig: {LeagueId}, Sezon: {Season} ({Count} takim)",
                leagueId, season, cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Futbol puan durumu cekiliyor. Lig: {LeagueId}, Sezon: {Season}", leagueId, season);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/standings?league={leagueId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Standings API yaniti (ilk 500 karakter): {Content}",
                content.Length > 500 ? content[..500] : content);
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Object && errors.EnumerateObject().Any())
            {
                foreach (var err in errors.EnumerateObject())
                    _logger.LogError("Standings API hatasi - {Key}: {Value}", err.Name, err.Value);
                return [];
            }

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array ||
                responseArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("Puan durumu yaniti beklenen formatta degil veya bos. Lig: {LeagueId}, Sezon: {Season}", leagueId, season);
                return [];
            }

            var leagueElement = responseArray[0].GetProperty("league");
            if (!leagueElement.TryGetProperty("standings", out var standingsOuter) ||
                standingsOuter.ValueKind != JsonValueKind.Array ||
                standingsOuter.GetArrayLength() == 0)
            {
                _logger.LogWarning("Puan durumu icin standings alani bulunamadi. Lig: {LeagueId}, Sezon: {Season}", leagueId, season);
                return [];
            }

            var standingsArray = standingsOuter[0];
            var result         = new List<LeagueStandingDto>();

            foreach (var item in standingsArray.EnumerateArray())
            {
                try
                {
                    var team     = item.GetProperty("team");
                    var statsAll = item.GetProperty("all");
                    var goals    = statsAll.GetProperty("goals");

                    var played       = statsAll.GetProperty("played").GetInt32();
                    var win          = statsAll.GetProperty("win").GetInt32();
                    var draw         = statsAll.GetProperty("draw").GetInt32();
                    var lose         = statsAll.GetProperty("lose").GetInt32();
                    var goalsFor     = goals.GetProperty("for").GetInt32();
                    var goalsAgainst = goals.GetProperty("against").GetInt32();
                    var points       = item.GetProperty("points").GetInt32();
                    var rank         = item.GetProperty("rank").GetInt32();
                    var teamLogo     = team.TryGetProperty("logo", out var logoEl) ? logoEl.GetString() : null;

                    result.Add(new LeagueStandingDto
                    {
                        Rank           = rank,
                        TeamName       = team.GetProperty("name").GetString() ?? string.Empty,
                        TeamLogo       = teamLogo,
                        Played         = played,
                        Won            = win,
                        Drawn          = draw,
                        Lost           = lose,
                        GoalsFor       = goalsFor,
                        GoalsAgainst   = goalsAgainst,
                        GoalDifference = goalsFor - goalsAgainst,
                        Points         = points
                    });
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "Puan durumu elemani parse edilirken hata olustu, atlaniyor.");
                }
            }

            var sorted = result.OrderBy(s => s.Rank).ToList();
            _standingsCache[cacheKey] = (sorted, DateTime.UtcNow.Add(StandingsCacheDuration));
            _logger.LogInformation("Puan durumu basariyla cekildi ve cache'lendi. Lig: {LeagueId}, Sezon: {Season}, {Count} takim",
                leagueId, season, sorted.Count);
            return sorted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Futbol puan durumu cekilirken hata olustu. Lig: {LeagueId}, Sezon: {Season}", leagueId, season);
            return _standingsCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }

    // =========================================================================
    // FUTBOL — Takım arama
    // =========================================================================

    public async Task<List<TeamSearchResultDto>> SearchFootballTeamsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/teams?search={Uri.EscapeDataString(query)}");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("response", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return [];
            var results = new List<TeamSearchResultDto>();
            foreach (var item in arr.EnumerateArray())
            {
                try
                {
                    var team    = item.GetProperty("team");
                    var country = item.TryGetProperty("venue", out _)
                        ? (team.TryGetProperty("country", out var c) ? c.GetString() : null)
                        : null;
                    results.Add(new TeamSearchResultDto
                    {
                        Id      = team.GetProperty("id").GetInt32(),
                        Name    = team.GetProperty("name").GetString() ?? "",
                        Logo    = team.TryGetProperty("logo", out var logo) ? logo.GetString() : null,
                        Country = team.TryGetProperty("country", out var cnt) ? cnt.GetString() : null,
                        Kind    = "team"
                    });
                }
                catch { }
            }
            return results.Take(10).ToList();
        }
        catch (Exception ex) { _logger.LogError(ex, "Futbol takım arama hatası"); return []; }
    }

    // =========================================================================
    // FUTBOL — Lig arama
    // =========================================================================

    public async Task<List<TeamSearchResultDto>> SearchFootballLeaguesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/leagues?search={Uri.EscapeDataString(query)}&type=league");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("response", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return [];
            var results = new List<TeamSearchResultDto>();
            foreach (var item in arr.EnumerateArray())
            {
                try
                {
                    var league  = item.GetProperty("league");
                    var countryEl = item.TryGetProperty("country", out var co) ? co : (JsonElement?)null;
                    results.Add(new TeamSearchResultDto
                    {
                        Id      = league.GetProperty("id").GetInt32(),
                        Name    = league.GetProperty("name").GetString() ?? "",
                        Logo    = league.TryGetProperty("logo", out var logo) ? logo.GetString() : null,
                        Country = countryEl?.TryGetProperty("name", out var cn) == true ? cn.GetString() : null,
                        Flag    = countryEl?.TryGetProperty("flag", out var fl) == true ? fl.GetString() : null,
                        Kind    = "league"
                    });
                }
                catch { }
            }
            return results.Take(10).ToList();
        }
        catch (Exception ex) { _logger.LogError(ex, "Futbol lig arama hatası"); return []; }
    }

    // =========================================================================
    // FUTBOL — Fikstür: takıma göre
    // =========================================================================

    public async Task<List<Match>> FetchFootballFixturesByTeamAsync(int teamId, int season)
    {
        var cacheKey = $"football_team_{teamId}_{season}";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Data;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/fixtures?team={teamId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("response", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return [];
            var matches = ParseFootballFixtures(arr);
            _matchCache[cacheKey] = (matches, DateTime.UtcNow.AddMinutes(30));
            return matches;
        }
        catch (Exception ex) { _logger.LogError(ex, "Futbol fikstür (takım) hatası. TeamId: {Id}", teamId); return []; }
    }

    // =========================================================================
    // FUTBOL — Fikstür: lige göre
    // =========================================================================

    public async Task<List<Match>> FetchFootballFixturesByLeagueAsync(int leagueId, int season)
    {
        var cacheKey = $"football_league_fixtures_{leagueId}_{season}";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Data;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/fixtures?league={leagueId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("response", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return [];
            var matches = ParseFootballFixtures(arr);
            _matchCache[cacheKey] = (matches, DateTime.UtcNow.AddMinutes(30));
            return matches;
        }
        catch (Exception ex) { _logger.LogError(ex, "Futbol fikstür (lig) hatası. LeagueId: {Id}", leagueId); return []; }
    }

    // =========================================================================
    // Yardımcı: DB MaxLength ile uyum (çok uzun isim/URL 500 üretmesin)
    // =========================================================================

    private static string TruncateRequired(string? s, int max) =>
        TruncateOptional(s, max) ?? string.Empty;

    private static string? TruncateOptional(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= max ? s : s[..max];
    }

    // =========================================================================
    // Yardımcı: futbol fixture array'ini Match listesine çevirir
    // =========================================================================

    private static List<Match> ParseFootballFixtures(JsonElement responseArray)
    {
        var matches = new List<Match>();

        foreach (var item in responseArray.EnumerateArray())
        {
            try
            {
                var fixture = item.GetProperty("fixture");
                var league  = item.GetProperty("league");
                var teams   = item.GetProperty("teams");
                var goals   = item.GetProperty("goals");

                var homeTeamObj = teams.GetProperty("home");
                var awayTeamObj = teams.GetProperty("away");

                var homeGoals = goals.GetProperty("home").ValueKind == JsonValueKind.Number
                    ? goals.GetProperty("home").GetInt32() : 0;
                var awayGoals = goals.GetProperty("away").ValueKind == JsonValueKind.Number
                    ? goals.GetProperty("away").GetInt32() : 0;

                var dateString = fixture.GetProperty("date").GetString();
                var startTime  = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out var parsedDate))
                    startTime = parsedDate;

                var statusObj   = fixture.GetProperty("status");
                var shortStatus = (statusObj.GetProperty("short").GetString() ?? string.Empty).ToUpperInvariant();
                var elapsed     = statusObj.TryGetProperty("elapsed", out var elapsedEl) &&
                                  elapsedEl.ValueKind == JsonValueKind.Number
                    ? elapsedEl.GetInt32() : 0;

                var status = shortStatus switch
                {
                    "FT" or "AET" or "PEN" or "FT_PEN" or "AWD" or "WO" or "CANC" or "ABD" => MatchStatus.Finished,
                    "NS" => MatchStatus.NotStarted,
                    "HT" or "BT" => MatchStatus.HalfTime,
                    _    => MatchStatus.Live
                };

                // API bazen bitmiş maçları NS döner (livescore yok, 48 saate kadar güncellenir)
                // Skor varsa ve maç saati geçmişse → Finished say
                if (status == MatchStatus.NotStarted && (homeGoals > 0 || awayGoals > 0) && startTime < DateTime.UtcNow.AddHours(-2))
                    status = MatchStatus.Finished;

                var fixtureId = fixture.GetProperty("id").GetInt32();

                matches.Add(new Match
                {
                    Id               = $"{SportType.Football}-{fixtureId}",
                    HomeTeam         = TruncateRequired(homeTeamObj.GetProperty("name").GetString(), 200),
                    AwayTeam         = TruncateRequired(awayTeamObj.GetProperty("name").GetString(), 200),
                    HomeScore        = homeGoals,
                    AwayScore        = awayGoals,
                    League           = TruncateRequired(league.GetProperty("name").GetString(), 200),
                    LeagueCountry    = TruncateOptional(league.TryGetProperty("country", out var lcEl) ? lcEl.GetString() : null, 100),
                    ExternalLeagueId = league.TryGetProperty("id", out var liEl) && liEl.ValueKind == JsonValueKind.Number ? liEl.GetInt32() : null,
                    LeagueFlag       = TruncateOptional(league.TryGetProperty("flag", out var lfEl) ? lfEl.GetString() : null, 500),
                    StartTime        = startTime,
                    Minute           = elapsed,
                    Status           = status,
                    SportType        = SportType.Football,
                    ExternalFixtureId = fixtureId,
                    HomeTeamLogo     = TruncateOptional(homeTeamObj.TryGetProperty("logo", out var hLogo) ? hLogo.GetString() : null, 500),
                    AwayTeamLogo     = TruncateOptional(awayTeamObj.TryGetProperty("logo", out var aLogo) ? aLogo.GetString() : null, 500)
                });
            }
            catch (Exception)
            {
                // Hatalı item'ı atla
            }
        }

        return matches;
    }

    // ── CollectAPI: Son hafta maç sonuçları (opsiyonel tarih: yyyy-MM-dd) ─────────
    public async Task<List<MatchResultDto>> FetchFootballResultsFromCollectApiAsync(string leagueKey, string? date = null)
    {
        var dateTag = string.IsNullOrWhiteSpace(date) ? "latest" : date.Trim();
        var cacheKey = $"collectapi_results_{leagueKey}_{dateTag}";
        if (_resultsCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("CollectAPI sonuçlar CACHE'ten: {LeagueKey} ({Count} maç)", leagueKey, cached.Data.Count);
            return cached.Data;
        }

        try
        {
            var collectApiKey = _configuration["CollectApi:ApiKey"]
                ?? throw new InvalidOperationException("CollectApi:ApiKey configuration is missing.");

            var url = $"https://api.collectapi.com/football/results?league={Uri.EscapeDataString(leagueKey)}";
            if (!string.IsNullOrWhiteSpace(date))
                url += $"&date={Uri.EscapeDataString(date.Trim())}";

            HttpResponseMessage response;
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                req.Headers.Add("authorization", $"apikey {collectApiKey}");
                response = await _httpClient.SendAsync(req);
            }

            for (var attempt = 0; attempt < 4 && response.StatusCode == HttpStatusCode.TooManyRequests; attempt++)
            {
                var waitMs = 900 * (attempt + 1);
                _logger.LogWarning("CollectAPI 429. Lig: {LeagueKey}, bekleme {WaitMs}ms", leagueKey, waitMs);
                await Task.Delay(waitMs);
                using var retryReq = new HttpRequestMessage(HttpMethod.Get, url);
                retryReq.Headers.Add("authorization", $"apikey {collectApiKey}");
                response.Dispose();
                response = await _httpClient.SendAsync(retryReq);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CollectAPI HTTP {Code} — {LeagueKey}", (int)response.StatusCode, leagueKey);
                response.Dispose();
                return _resultsCache.TryGetValue(cacheKey, out var staleErr) ? staleErr.Data : [];
            }

            var content = await response.Content.ReadAsStringAsync();
            response.Dispose();
            _logger.LogInformation("CollectAPI results yanıtı ({LeagueKey}): {Preview}",
                leagueKey, content.Length > 200 ? content[..200] : content);

            using var doc = JsonDocument.Parse(content);

            // Yanıt doğrudan array veya {success, result:[...]} olabilir
            JsonElement items;
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                items = doc.RootElement;
            }
            else if (doc.RootElement.TryGetProperty("result", out var resultEl)
                     && resultEl.ValueKind == JsonValueKind.Array)
            {
                items = resultEl;
            }
            else
            {
                _logger.LogWarning("CollectAPI results: beklenmeyen format. League: {LeagueKey}", leagueKey);
                return [];
            }

            var results = new List<MatchResultDto>();
            foreach (var item in items.EnumerateArray())
            {
                var home      = item.TryGetProperty("home",  out var h) ? h.GetString() ?? "" : "";
                var away      = item.TryGetProperty("away",  out var a) ? a.GetString() ?? "" : "";
                var skorRaw = item.TryGetProperty("skor", out var s) ? s.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(skorRaw) && item.TryGetProperty("score", out var sc))
                    skorRaw = sc.GetString() ?? "";
                var matchDate = item.TryGetProperty("date", out var d) ? d.GetString() ?? "" : "";

                int? homeScore = null, awayScore = null;
                bool isPlayed  = false;

                if (!string.IsNullOrEmpty(skorRaw) && !skorRaw.Contains("undefined"))
                {
                    var parts = skorRaw.Split('-');
                    if (parts.Length == 2
                        && int.TryParse(parts[0].Trim(), out var hs)
                        && int.TryParse(parts[1].Trim(), out var as_))
                    {
                        homeScore = hs;
                        awayScore = as_;
                        isPlayed  = true;
                    }
                }

                results.Add(new MatchResultDto
                {
                    HomeTeam  = home,
                    AwayTeam  = away,
                    HomeScore = homeScore,
                    AwayScore = awayScore,
                    Date      = matchDate,
                    IsPlayed  = isPlayed
                });
            }

            _resultsCache[cacheKey] = (results, DateTime.UtcNow.Add(ResultsCacheDuration));
            _logger.LogInformation("CollectAPI results başarılı: {LeagueKey}, {Count} maç", leagueKey, results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CollectAPI results hatası: {LeagueKey}", leagueKey);
            return _resultsCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }
}
