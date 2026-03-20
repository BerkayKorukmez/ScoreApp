using SkorTakip.API.Models;
using System.Text.Json;

namespace SkorTakip.API.Services;

public partial class ExternalApiService
{
    // =========================================================================
    // TENİS — Canlı maçlar
    // =========================================================================

    public async Task<List<Match>> FetchTennisMatchesAsync()
    {
        const string cacheKey = "tennis";
        if (_matchCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Tenis maclari CACHE'ten donduruluyor ({Count} mac)", cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Tenis maclari API-Sports uzerinden cekiliyor...");

            var request = new HttpRequestMessage(HttpMethod.Get,
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
                return [];
            }

            var matches = new List<Match>();

            foreach (var item in responseArray.EnumerateArray())
            {
                try
                {
                    var matchInfo   = item.GetProperty("match");
                    var league      = item.GetProperty("league");
                    var teams       = item.GetProperty("teams");
                    var homeTeamObj = teams.GetProperty("home");
                    var awayTeamObj = teams.GetProperty("away");

                    var dateString = matchInfo.GetProperty("date").GetString();
                    var startTime  = DateTime.UtcNow;
                    if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out var parsedDate))
                        startTime = parsedDate;

                    var statusObj   = matchInfo.GetProperty("status");
                    var shortStatus = statusObj.GetProperty("short").GetString() ?? string.Empty;

                    var status = shortStatus switch
                    {
                        "FT" => MatchStatus.Finished,
                        "NS" => MatchStatus.NotStarted,
                        _    => MatchStatus.Live
                    };

                    var tMatchId = matchInfo.GetProperty("id").GetInt32();

                    matches.Add(new Match
                    {
                        Id               = $"{SportType.Tennis}-{tMatchId}",
                        HomeTeam         = homeTeamObj.GetProperty("name").GetString() ?? string.Empty,
                        AwayTeam         = awayTeamObj.GetProperty("name").GetString() ?? string.Empty,
                        League           = league.GetProperty("name").GetString() ?? string.Empty,
                        LeagueCountry    = league.TryGetProperty("country", out var lcEl) ? lcEl.GetString() : null,
                        ExternalLeagueId = league.TryGetProperty("id", out var liEl) && liEl.ValueKind == JsonValueKind.Number ? liEl.GetInt32() : null,
                        LeagueFlag       = league.TryGetProperty("flag", out var lfEl) ? lfEl.GetString() : null,
                        StartTime        = startTime,
                        Status           = status,
                        SportType        = SportType.Tennis,
                        HomeTeamLogo     = homeTeamObj.TryGetProperty("logo", out var hLogo) ? hLogo.GetString() : null,
                        AwayTeamLogo     = awayTeamObj.TryGetProperty("logo", out var aLogo) ? aLogo.GetString() : null
                    });
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "Tenis maci parse edilirken hata olustu, atlaniyor.");
                }
            }

            _matchCache[cacheKey] = (matches, DateTime.UtcNow.Add(MatchCacheDuration));
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenis maclari cekilirken hata olustu");
            return _matchCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }
}
