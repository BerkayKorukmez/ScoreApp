using SkorTakip.API.DTOs;
using SkorTakip.API.Models;
using System.Text.Json;

namespace SkorTakip.API.Services;

public partial class ExternalApiService
{
    // =========================================================================
    // VOLEYBOL — Günlük maçlar
    // =========================================================================

    public async Task<List<Match>> FetchVolleyballMatchesAsync()
    {
        const string cacheKey = "volleyball";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Voleybol maclari CACHE'ten donduruluyor ({Count} mac)", cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Voleybol maclari API-Sports uzerinden cekiliyor...");

            var today   = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.volleyball.api-sports.io/games?date={today}");
            request.Headers.Add("x-apisports-key", GetApiKey("Volleyball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Voleybol API yaniti (ilk 300 karakter): {Content}",
                content.Length > 300 ? content[..300] : content);
            using var doc = JsonDocument.Parse(content);

            if (HasApiErrors(doc.RootElement, "Voleybol"))
                return [];

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("API-Sports voleybol yaniti beklenen formatta degil.");
                return [];
            }

            _logger.LogInformation("Voleybol: API'den {Count} mac alindi.", responseArray.GetArrayLength());
            var matches = ParseVolleyballGames(responseArray);

            _matchCache[cacheKey] = (matches, DateTime.UtcNow.Add(MatchCacheDuration));
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voleybol maclari cekilirken hata olustu");
            return _matchCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }

    // =========================================================================
    // VOLEYBOL — Tarih bazlı geçmiş maçlar
    // =========================================================================

    public async Task<List<Match>> FetchVolleyballMatchesByDateAsync(string date)
    {
        var cacheKey = $"volleyball_history_{date}";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Voleybol gecmis maclari CACHE'ten donduruluyor. Tarih: {Date} ({Count} mac)", date, cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Voleybol gecmis maclari API'den cekiliyor. Tarih: {Date}", date);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.volleyball.api-sports.io/games?date={date}");
            request.Headers.Add("x-apisports-key", GetApiKey("Volleyball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (HasApiErrors(doc.RootElement, "Voleybol History"))
                return [];

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
                return [];

            _logger.LogInformation("Voleybol gecmis: {Count} mac alindi. Tarih: {Date}", responseArray.GetArrayLength(), date);
            var matches = ParseVolleyballGames(responseArray);

            _matchCache[cacheKey] = (matches, DateTime.UtcNow.AddMinutes(60));
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voleybol gecmis maclari cekilirken hata. Tarih: {Date}", date);
            return _matchCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }

    /// <summary>
    /// API-Sports game ID ile tek maç (geçmiş maç detayı).
    /// </summary>
    public async Task<Match?> FetchVolleyballMatchByGameIdAsync(int gameId)
    {
        try
        {
            _logger.LogInformation("Voleybol tek mac cekiliyor. Id: {Id}", gameId);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.volleyball.api-sports.io/games?id={gameId}");
            request.Headers.Add("x-apisports-key", GetApiKey("Volleyball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (HasApiErrors(doc.RootElement, "Voleybol tek mac"))
                return null;

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array ||
                responseArray.GetArrayLength() < 1)
            {
                _logger.LogWarning("Voleybol mac bulunamadi. Id: {Id}", gameId);
                return null;
            }

            return ParseVolleyballGames(responseArray).FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voleybol tek mac cekilemedi. Id: {Id}", gameId);
            return null;
        }
    }

    // =========================================================================
    // VOLEYBOL — Maç istatistikleri (set skorları)
    // =========================================================================

    public async Task<Dictionary<string, object>?> FetchVolleyballMatchStatisticsAsync(int gameId)
    {
        var cacheKey = -gameId - 2000;
        if (_statsCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Voleybol istatistikleri CACHE'ten donduruluyor - Game ID: {GameId}", gameId);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Voleybol mac istatistikleri cekiliyor - Game ID: {GameId}", gameId);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.volleyball.api-sports.io/games?id={gameId}");
            request.Headers.Add("x-apisports-key", GetApiKey("Volleyball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array ||
                responseArray.GetArrayLength() < 1)
            {
                _logger.LogWarning("Voleybol istatistik verisi bulunamadi - Game ID: {GameId}", gameId);
                return null;
            }

            var game  = responseArray[0];
            var stats = new Dictionary<string, object> { { "sportType", "volleyball" } };

            if (game.TryGetProperty("periods", out var periods))
            {
                var sets      = new[] { "first", "second", "third", "fourth", "fifth" };
                var setLabels = new[] { "Set1", "Set2", "Set3", "Set4", "Set5" };

                for (int i = 0; i < sets.Length; i++)
                {
                    if (periods.TryGetProperty(sets[i], out var setPeriod) &&
                        setPeriod.TryGetProperty("home", out var sh) && sh.ValueKind == JsonValueKind.Number &&
                        setPeriod.TryGetProperty("away", out var sa) && sa.ValueKind == JsonValueKind.Number)
                    {
                        stats[$"home{setLabels[i]}"] = sh.GetInt32();
                        stats[$"away{setLabels[i]}"] = sa.GetInt32();
                    }
                }
            }

            if (game.TryGetProperty("scores", out var scores))
            {
                var homeScore = scores.GetProperty("home");
                var awayScore = scores.GetProperty("away");
                if (homeScore.ValueKind == JsonValueKind.Number) stats["homeTotal"] = homeScore.GetInt32();
                if (awayScore.ValueKind == JsonValueKind.Number) stats["awayTotal"] = awayScore.GetInt32();
            }

            _statsCache[cacheKey] = (stats, DateTime.UtcNow.Add(StatsCacheDuration));
            _logger.LogInformation("Voleybol istatistikleri basariyla cekildi - Game ID: {GameId}", gameId);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voleybol istatistikleri cekilirken hata olustu - Game ID: {GameId}", gameId);
            return _statsCache.TryGetValue(cacheKey, out var stale) ? stale.Data : null;
        }
    }

    // =========================================================================
    // VOLEYBOL — Puan durumu
    // =========================================================================

    public async Task<List<LeagueStandingDto>> FetchVolleyballStandingsAsync(int leagueId, int season)
    {
        var cacheKey = $"vball_standings_{leagueId}_{season}";
        if (_standingsCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Voleybol puan durumu CACHE'ten donduruluyor. Lig: {LeagueId}", leagueId);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Voleybol puan durumu cekiliyor. Lig: {LeagueId}, Sezon: {Season}", leagueId, season);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.volleyball.api-sports.io/standings?league={leagueId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Volleyball"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array ||
                responseArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("Voleybol puan durumu bos dondu. Lig: {LeagueId}", leagueId);
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

                    int setsWon = 0;
                    int setsLost = 0;
                    if (item.TryGetProperty("sets", out var sets))
                    {
                        if (sets.TryGetProperty("win", out var sw) && sw.ValueKind == JsonValueKind.Number)
                            setsWon = sw.GetInt32();
                        if (sets.TryGetProperty("lose", out var sl) && sl.ValueKind == JsonValueKind.Number)
                            setsLost = sl.GetInt32();
                    }

                    var totalPoints = 0;
                    if (item.TryGetProperty("points", out var pts))
                    {
                        if (pts.TryGetProperty("total", out var ptTotal) && ptTotal.ValueKind == JsonValueKind.Number)
                            totalPoints = ptTotal.GetInt32();
                        else if (pts.TryGetProperty("for", out var ptFor) && ptFor.ValueKind == JsonValueKind.Number)
                            totalPoints = ptFor.GetInt32();
                    }
                    if (totalPoints == 0) totalPoints = won;

                    result.Add(new LeagueStandingDto
                    {
                        Rank           = position,
                        TeamName       = teamName,
                        TeamLogo       = teamLogo,
                        Played         = played,
                        Won            = won,
                        Drawn          = 0,
                        Lost           = lost,
                        GoalsFor       = setsWon,
                        GoalsAgainst   = setsLost,
                        GoalDifference = setsWon - setsLost,
                        Points         = totalPoints
                    });
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "Voleybol standings item parse hatasi, atlaniyor.");
                }
            }

            var sorted = result.OrderBy(s => s.Rank).ToList();
            _standingsCache[cacheKey] = (sorted, DateTime.UtcNow.Add(StandingsCacheDuration));
            _logger.LogInformation("Voleybol puan durumu cekildi. Lig: {LeagueId}, {Count} takim", leagueId, sorted.Count);
            return sorted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voleybol puan durumu cekilirken hata. Lig: {LeagueId}", leagueId);
            return _standingsCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }

    // =========================================================================
    // VOLEYBOL — Takım / Lig arama
    // =========================================================================

    public async Task<List<TeamSearchResultDto>> SearchVolleyballTeamsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.volleyball.api-sports.io/teams?search={Uri.EscapeDataString(query)}");
            request.Headers.Add("x-apisports-key", GetApiKey("Volleyball"));
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
        catch (Exception ex) { _logger.LogError(ex, "Voleybol takım arama hatası"); return []; }
    }

    public async Task<List<TeamSearchResultDto>> SearchVolleyballLeaguesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.volleyball.api-sports.io/leagues?search={Uri.EscapeDataString(query)}");
            request.Headers.Add("x-apisports-key", GetApiKey("Volleyball"));
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
        catch (Exception ex) { _logger.LogError(ex, "Voleybol lig arama hatası"); return []; }
    }

    // =========================================================================
    // VOLEYBOL — Fikstür: takıma / lige göre
    // =========================================================================

    public async Task<List<Match>> FetchVolleyballFixturesByTeamAsync(int teamId, int season)
    {
        var cacheKey = $"volleyball_team_{teamId}_{season}";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Data;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.volleyball.api-sports.io/games?team={teamId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Volleyball"));
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("response", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return [];
            var matches = ParseVolleyballGames(arr);
            _matchCache[cacheKey] = (matches, DateTime.UtcNow.AddMinutes(30));
            return matches;
        }
        catch (Exception ex) { _logger.LogError(ex, "Voleybol fikstür (takım) hatası. TeamId: {Id}", teamId); return []; }
    }

    public async Task<List<Match>> FetchVolleyballFixturesByLeagueAsync(int leagueId, int season)
    {
        var cacheKey = $"volleyball_league_fixtures_{leagueId}_{season}";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Data;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v1.volleyball.api-sports.io/games?league={leagueId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Volleyball"));
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("response", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return [];
            var matches = ParseVolleyballGames(arr);
            _matchCache[cacheKey] = (matches, DateTime.UtcNow.AddMinutes(30));
            return matches;
        }
        catch (Exception ex) { _logger.LogError(ex, "Voleybol fikstür (lig) hatası. LeagueId: {Id}", leagueId); return []; }
    }

    // =========================================================================
    // Yardımcı: volleyball games array'ini Match listesine çevirir
    // =========================================================================

    private static List<Match> ParseVolleyballGames(JsonElement responseArray)
    {
        var matches = new List<Match>();

        foreach (var item in responseArray.EnumerateArray())
        {
            try
            {
                var league      = item.GetProperty("league");
                var teams       = item.GetProperty("teams");
                var homeTeamObj = teams.GetProperty("home");
                var awayTeamObj = teams.GetProperty("away");

                int homeScore = 0, awayScore = 0;
                if (item.TryGetProperty("scores", out var scores))
                {
                    var homeEl = scores.GetProperty("home");
                    var awayEl = scores.GetProperty("away");
                    homeScore  = homeEl.ValueKind == JsonValueKind.Number ? homeEl.GetInt32() : 0;
                    awayScore  = awayEl.ValueKind == JsonValueKind.Number ? awayEl.GetInt32() : 0;
                }

                var dateString = item.GetProperty("date").GetString();
                var startTime  = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out var parsedDate))
                    startTime = parsedDate;

                var statusObj   = item.GetProperty("status");
                var shortStatus = statusObj.GetProperty("short").GetString() ?? string.Empty;

                var status = shortStatus switch
                {
                    "FT" => MatchStatus.Finished,
                    "NS" => MatchStatus.NotStarted,
                    _    => MatchStatus.Live
                };

                var gameId = item.GetProperty("id").GetInt32();

                matches.Add(new Match
                {
                    Id               = $"{SportType.Volleyball}-{gameId}",
                    HomeTeam         = homeTeamObj.GetProperty("name").GetString() ?? string.Empty,
                    AwayTeam         = awayTeamObj.GetProperty("name").GetString() ?? string.Empty,
                    HomeScore        = homeScore,
                    AwayScore        = awayScore,
                    League           = league.GetProperty("name").GetString() ?? string.Empty,
                    LeagueCountry    = league.TryGetProperty("country", out var lcEl) ? lcEl.GetString() : null,
                    ExternalLeagueId = league.TryGetProperty("id", out var liEl) && liEl.ValueKind == JsonValueKind.Number ? liEl.GetInt32() : null,
                    LeagueFlag       = league.TryGetProperty("flag", out var lfEl) ? lfEl.GetString() : null,
                    StartTime        = startTime,
                    Status           = status,
                    SportType        = SportType.Volleyball,
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
}
