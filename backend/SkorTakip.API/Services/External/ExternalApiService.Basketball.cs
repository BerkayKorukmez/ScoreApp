using SkorTakip.API.DTOs;
using SkorTakip.API.Models;
using System.Text.Json;

namespace SkorTakip.API.Services;

public partial class ExternalApiService
{
    // =========================================================================
    // BASKETBOL — Günlük maçlar
    // =========================================================================

    public async Task<List<Match>> FetchBasketballMatchesAsync()
    {
        const string cacheKey = "basketball";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Basketbol maclari CACHE'ten donduruluyor ({Count} mac)", cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Basketbol maclari API-Sports uzerinden cekiliyor...");

            var today   = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.basketball.api-sports.io/games?date={today}");
            request.Headers.Add("x-apisports-key", GetApiKey("Basketball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Basketbol API yaniti (ilk 300 karakter): {Content}",
                content.Length > 300 ? content[..300] : content);
            using var doc = JsonDocument.Parse(content);

            if (HasApiErrors(doc.RootElement, "Basketbol"))
                return [];

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("API-Sports basketbol yaniti beklenen formatta degil.");
                return [];
            }

            _logger.LogInformation("Basketbol: API'den {Count} mac alindi.", responseArray.GetArrayLength());
            var matches = ParseBasketballGames(responseArray);

            _matchCache[cacheKey] = (matches, DateTime.UtcNow.Add(MatchCacheDuration));
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Basketbol maclari cekilirken hata olustu");
            return _matchCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }

    // =========================================================================
    // BASKETBOL — Tarih bazlı geçmiş maçlar
    // =========================================================================

    public async Task<List<Match>> FetchBasketballMatchesByDateAsync(string date)
    {
        var cacheKey = $"basketball_history_{date}";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Basketbol gecmis maclari CACHE'ten donduruluyor. Tarih: {Date} ({Count} mac)", date, cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Basketbol gecmis maclari API'den cekiliyor. Tarih: {Date}", date);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.basketball.api-sports.io/games?date={date}");
            request.Headers.Add("x-apisports-key", GetApiKey("Basketball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (HasApiErrors(doc.RootElement, "Basketbol History"))
                return [];

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
                return [];

            _logger.LogInformation("Basketbol gecmis: {Count} mac alindi. Tarih: {Date}", responseArray.GetArrayLength(), date);
            var matches = ParseBasketballGames(responseArray);

            _matchCache[cacheKey] = (matches, DateTime.UtcNow.AddMinutes(60));
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Basketbol gecmis maclari cekilirken hata. Tarih: {Date}", date);
            return _matchCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }

    /// <summary>
    /// API-Sports game ID ile tek maç (geçmiş maç detayı).
    /// </summary>
    public async Task<Match?> FetchBasketballMatchByGameIdAsync(int gameId)
    {
        try
        {
            _logger.LogInformation("Basketbol tek mac cekiliyor. Id: {Id}", gameId);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.basketball.api-sports.io/games?id={gameId}");
            request.Headers.Add("x-apisports-key", GetApiKey("Basketball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (HasApiErrors(doc.RootElement, "Basketbol tek mac"))
                return null;

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array ||
                responseArray.GetArrayLength() < 1)
            {
                _logger.LogWarning("Basketbol mac bulunamadi. Id: {Id}", gameId);
                return null;
            }

            return ParseBasketballGames(responseArray).FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Basketbol tek mac cekilemedi. Id: {Id}", gameId);
            return null;
        }
    }

    // =========================================================================
    // BASKETBOL — Maç istatistikleri (çeyrek skorları)
    // =========================================================================

    public async Task<Dictionary<string, object>?> FetchBasketballMatchStatisticsAsync(int gameId)
    {
        // Basketbol istatistikleri için negatif offset ile çakışmayı önle
        var cacheKey = -gameId - 1000;
        if (_statsCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Basketbol istatistikleri CACHE'ten donduruluyor - Game ID: {GameId}", gameId);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Basketbol mac istatistikleri cekiliyor - Game ID: {GameId}", gameId);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.basketball.api-sports.io/games?id={gameId}");
            request.Headers.Add("x-apisports-key", GetApiKey("Basketball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array ||
                responseArray.GetArrayLength() < 1)
            {
                _logger.LogWarning("Basketbol istatistik verisi bulunamadi - Game ID: {GameId}", gameId);
                return null;
            }

            var game       = responseArray[0];
            var scores     = game.GetProperty("scores");
            var homeScores = scores.GetProperty("home");
            var awayScores = scores.GetProperty("away");

            var stats = new Dictionary<string, object> { { "sportType", "basketball" } };

            var quarters      = new[] { "quarter_1", "quarter_2", "quarter_3", "quarter_4" };
            var quarterLabels = new[] { "Q1", "Q2", "Q3", "Q4" };

            for (int i = 0; i < quarters.Length; i++)
            {
                if (homeScores.TryGetProperty(quarters[i], out var hq) && hq.ValueKind == JsonValueKind.Number &&
                    awayScores.TryGetProperty(quarters[i], out var aq) && aq.ValueKind == JsonValueKind.Number)
                {
                    stats[$"home{quarterLabels[i]}"] = hq.GetInt32();
                    stats[$"away{quarterLabels[i]}"] = aq.GetInt32();
                }
            }

            if (homeScores.TryGetProperty("over_time", out var hot) && hot.ValueKind == JsonValueKind.Number &&
                awayScores.TryGetProperty("over_time", out var aot) && aot.ValueKind == JsonValueKind.Number)
            {
                stats["homeOT"] = hot.GetInt32();
                stats["awayOT"] = aot.GetInt32();
            }

            if (homeScores.TryGetProperty("total", out var ht) && ht.ValueKind == JsonValueKind.Number &&
                awayScores.TryGetProperty("total", out var at) && at.ValueKind == JsonValueKind.Number)
            {
                stats["homeTotal"] = ht.GetInt32();
                stats["awayTotal"] = at.GetInt32();
            }

            if (game.TryGetProperty("status", out var statusObj) &&
                statusObj.TryGetProperty("halftime", out var ht2) &&
                ht2.ValueKind == JsonValueKind.String)
                stats["halftime"] = ht2.GetString()!;

            _statsCache[cacheKey] = (stats, DateTime.UtcNow.Add(StatsCacheDuration));
            _logger.LogInformation("Basketbol istatistikleri basariyla cekildi - Game ID: {GameId}", gameId);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Basketbol istatistikleri cekilirken hata olustu - Game ID: {GameId}", gameId);
            return _statsCache.TryGetValue(cacheKey, out var stale) ? stale.Data : null;
        }
    }

    // =========================================================================
    // BASKETBOL — Puan durumu
    // =========================================================================

    public async Task<List<LeagueStandingDto>> FetchBasketballStandingsAsync(int leagueId, string season)
    {
        var cacheKey = $"bball_standings_{leagueId}_{season}";
        if (_standingsCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Basketbol puan durumu CACHE'ten donduruluyor. Lig: {LeagueId}", leagueId);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Basketbol puan durumu cekiliyor. Lig: {LeagueId}, Sezon: {Season}", leagueId, season);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.basketball.api-sports.io/standings?league={leagueId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Basketball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array ||
                responseArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("Basketbol puan durumu bos dondu. Lig: {LeagueId}", leagueId);
                return [];
            }

            var result = new List<LeagueStandingDto>();

            foreach (var item in responseArray.EnumerateArray())
            {
                try
                {
                    var position = item.TryGetProperty("position", out var pos) ? pos.GetInt32() : 0;
                    var team     = item.GetProperty("team");
                    var teamName = team.GetProperty("name").GetString() ?? string.Empty;
                    var teamLogo = team.TryGetProperty("logo", out var logo) ? logo.GetString() : null;

                    var games  = item.GetProperty("games");
                    var played = games.TryGetProperty("played", out var pl) ? pl.GetInt32() : 0;
                    var won    = games.TryGetProperty("win", out var w) && w.TryGetProperty("total", out var wt) ? wt.GetInt32() : 0;
                    var lost   = games.TryGetProperty("lose", out var l) && l.TryGetProperty("total", out var lt) ? lt.GetInt32() : 0;

                    int ptsFor = 0;
                    int ptsAgainst = 0;
                    if (item.TryGetProperty("points", out var points))
                    {
                        if (points.TryGetProperty("for", out var pf) && pf.ValueKind == JsonValueKind.Number)
                            ptsFor = pf.GetInt32();
                        if (points.TryGetProperty("against", out var pa) && pa.ValueKind == JsonValueKind.Number)
                            ptsAgainst = pa.GetInt32();
                    }

                    result.Add(new LeagueStandingDto
                    {
                        Rank           = position,
                        TeamName       = teamName,
                        TeamLogo       = teamLogo,
                        Played         = played,
                        Won            = won,
                        Drawn          = 0,
                        Lost           = lost,
                        GoalsFor       = ptsFor,
                        GoalsAgainst   = ptsAgainst,
                        GoalDifference = ptsFor - ptsAgainst,
                        Points         = won  // Basketbolda puan = galibiyet sayısı
                    });
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "Basketbol standings item parse hatasi, atlaniyor.");
                }
            }

            var sorted = result.OrderBy(s => s.Rank).ToList();
            _standingsCache[cacheKey] = (sorted, DateTime.UtcNow.Add(StandingsCacheDuration));
            _logger.LogInformation("Basketbol puan durumu cekildi. Lig: {LeagueId}, {Count} takim", leagueId, sorted.Count);
            return sorted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Basketbol puan durumu cekilirken hata. Lig: {LeagueId}", leagueId);
            return _standingsCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }

    // =========================================================================
    // BASKETBOL — Takım / Lig arama
    // =========================================================================

    public async Task<List<TeamSearchResultDto>> SearchBasketballTeamsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.basketball.api-sports.io/teams?search={Uri.EscapeDataString(query)}");
            request.Headers.Add("x-apisports-key", GetApiKey("Basketball"));
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
                    results.Add(new TeamSearchResultDto
                    {
                        Id      = item.GetProperty("id").GetInt32(),
                        Name    = item.GetProperty("name").GetString() ?? "",
                        Logo    = item.TryGetProperty("logo", out var logo) ? logo.GetString() : null,
                        Country = item.TryGetProperty("country", out var cnt)
                                    ? (cnt.TryGetProperty("name", out var cn) ? cn.GetString() : null) : null,
                        Kind    = "team"
                    });
                }
                catch { }
            }
            return results.Take(10).ToList();
        }
        catch (Exception ex) { _logger.LogError(ex, "Basketbol takım arama hatası"); return []; }
    }

    public async Task<List<TeamSearchResultDto>> SearchBasketballLeaguesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.basketball.api-sports.io/leagues?search={Uri.EscapeDataString(query)}");
            request.Headers.Add("x-apisports-key", GetApiKey("Basketball"));
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
                    results.Add(new TeamSearchResultDto
                    {
                        Id      = item.GetProperty("id").GetInt32(),
                        Name    = item.GetProperty("name").GetString() ?? "",
                        Logo    = item.TryGetProperty("logo", out var logo) ? logo.GetString() : null,
                        Country = item.TryGetProperty("country", out var cnt)
                                    ? (cnt.TryGetProperty("name", out var cn) ? cn.GetString() : null) : null,
                        Kind    = "league"
                    });
                }
                catch { }
            }
            return results.Take(10).ToList();
        }
        catch (Exception ex) { _logger.LogError(ex, "Basketbol lig arama hatası"); return []; }
    }

    // =========================================================================
    // BASKETBOL — Fikstür: takıma / lige göre
    // =========================================================================

    public async Task<List<Match>> FetchBasketballFixturesByTeamAsync(int teamId, string season)
    {
        var cacheKey = $"basketball_team_{teamId}_{season}";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Data;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.basketball.api-sports.io/games?team={teamId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Basketball"));
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("response", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return [];
            var matches = ParseBasketballGames(arr);
            _matchCache[cacheKey] = (matches, DateTime.UtcNow.AddMinutes(30));
            return matches;
        }
        catch (Exception ex) { _logger.LogError(ex, "Basketbol fikstür (takım) hatası. TeamId: {Id}", teamId); return []; }
    }

    public async Task<List<Match>> FetchBasketballFixturesByLeagueAsync(int leagueId, string season)
    {
        var cacheKey = $"basketball_league_fixtures_{leagueId}_{season}";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Data;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.basketball.api-sports.io/games?league={leagueId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Basketball"));
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("response", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return [];
            var matches = ParseBasketballGames(arr);
            _matchCache[cacheKey] = (matches, DateTime.UtcNow.AddMinutes(30));
            return matches;
        }
        catch (Exception ex) { _logger.LogError(ex, "Basketbol fikstür (lig) hatası. LeagueId: {Id}", leagueId); return []; }
    }

    // =========================================================================
    // Yardımcı: basketball games array'ini Match listesine çevirir
    // =========================================================================

    private static List<Match> ParseBasketballGames(JsonElement responseArray)
    {
        var matches = new List<Match>();

        foreach (var item in responseArray.EnumerateArray())
        {
            try
            {
                var league = item.GetProperty("league");
                var teams  = item.GetProperty("teams");
                var scores = item.GetProperty("scores");

                var homeTeamObj = teams.GetProperty("home");
                var awayTeamObj = teams.GetProperty("away");

                var homeScoreEl = scores.GetProperty("home").GetProperty("total");
                var awayScoreEl = scores.GetProperty("away").GetProperty("total");

                var dateString = item.GetProperty("date").GetString();
                var startTime  = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out var parsedDate))
                    startTime = parsedDate;

                var statusObj   = item.GetProperty("status");
                var shortStatus = statusObj.GetProperty("short").GetString() ?? string.Empty;

                var status = shortStatus switch
                {
                    "FT" or "AOT" => MatchStatus.Finished,
                    "NS"          => MatchStatus.NotStarted,
                    "HT" or "BT"  => MatchStatus.HalfTime,
                    _             => MatchStatus.Live
                };

                var gameId = item.GetProperty("id").GetInt32();

                matches.Add(new Match
                {
                    Id               = $"{SportType.Basketball}-{gameId}",
                    HomeTeam         = homeTeamObj.GetProperty("name").GetString() ?? string.Empty,
                    AwayTeam         = awayTeamObj.GetProperty("name").GetString() ?? string.Empty,
                    HomeScore        = homeScoreEl.ValueKind == JsonValueKind.Number ? homeScoreEl.GetInt32() : 0,
                    AwayScore        = awayScoreEl.ValueKind == JsonValueKind.Number ? awayScoreEl.GetInt32() : 0,
                    League           = league.GetProperty("name").GetString() ?? string.Empty,
                    LeagueCountry    = league.TryGetProperty("country", out var lcEl) ? lcEl.GetString() : null,
                    ExternalLeagueId = league.TryGetProperty("id", out var liEl) && liEl.ValueKind == JsonValueKind.Number ? liEl.GetInt32() : null,
                    LeagueFlag       = league.TryGetProperty("flag", out var lfEl) ? lfEl.GetString() : null,
                    StartTime        = startTime,
                    Status           = status,
                    SportType        = SportType.Basketball,
                    ExternalFixtureId = gameId,
                    HomeTeamLogo     = homeTeamObj.TryGetProperty("logo", out var hLogo) ? hLogo.GetString() : null,
                    AwayTeamLogo     = awayTeamObj.TryGetProperty("logo", out var aLogo) ? aLogo.GetString() : null
                });
            }
            catch (Exception)
            {
                // Hatalı item'ı atla
            }
        }

        return matches;
    }

    // =========================================================================
    // Ortak yardımcı: API hata objesini kontrol eder
    // =========================================================================

    private bool HasApiErrors(JsonElement root, string context)
    {
        if (!root.TryGetProperty("errors", out var errors)) return false;

        if (errors.ValueKind == JsonValueKind.Object && errors.EnumerateObject().Any())
        {
            foreach (var err in errors.EnumerateObject())
                _logger.LogError("{Context} API hatasi - {Key}: {Value}", context, err.Name, err.Value);
            return true;
        }

        if (errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
        {
            _logger.LogError("{Context} API hatasi: {Errors}", context, errors.ToString());
            return true;
        }

        return false;
    }
}
