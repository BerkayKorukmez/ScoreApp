using System.Collections.Concurrent;
using SkorTakip.API.DTOs;
using System.Text.Json;

namespace SkorTakip.API.Services;

/// <summary>
/// Gol krallığı — API-Sports /players/topscorers endpoint'i.
/// </summary>
public partial class ExternalApiService
{
    private static readonly ConcurrentDictionary<string, (List<GoalKingDto> Data, DateTime ExpiresAt)> _goalKingsCache = new();
    private static readonly TimeSpan GoalKingsCacheDuration = TimeSpan.FromMinutes(30);

    public async Task<List<GoalKingDto>> FetchGoalKingsAsync(int leagueId, int season)
    {
        var cacheKey = $"goalkings_{leagueId}_{season}";
        if (_goalKingsCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Gol krallari CACHE: leagueId={LeagueId}, season={Season} ({Count})", leagueId, season, cached.Data.Count);
            return cached.Data;
        }

        try
        {
            _logger.LogInformation("Gol krallari API-Sports'tan cekiliyor: leagueId={LeagueId}, season={Season}", leagueId, season);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/players/topscorers?league={leagueId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Gol krallari API yaniti (ilk 300 karakter): {Content}",
                content.Length > 300 ? content[..300] : content);

            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Object && errors.EnumerateObject().Any())
            {
                foreach (var err in errors.EnumerateObject())
                    _logger.LogError("Gol krallari API hatasi - {Key}: {Value}", err.Name, err.Value);
                return [];
            }

            if (!doc.RootElement.TryGetProperty("response", out var responseArray) ||
                responseArray.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Gol krallari API beklenmeyen format: leagueId={LeagueId}", leagueId);
                return [];
            }

            var list = new List<GoalKingDto>();
            foreach (var item in responseArray.EnumerateArray())
            {
                try
                {
                    var playerId   = 0;
                    string playerName = "";
                    string? playerPhoto = null;

                    if (item.TryGetProperty("player", out var playerEl))
                    {
                        if (playerEl.TryGetProperty("id",    out var idEl)    && idEl.ValueKind    == JsonValueKind.Number) playerId   = idEl.GetInt32();
                        if (playerEl.TryGetProperty("name",  out var nameEl)  && nameEl.ValueKind  == JsonValueKind.String) playerName = nameEl.GetString() ?? "";
                        if (playerEl.TryGetProperty("photo", out var photoEl) && photoEl.ValueKind == JsonValueKind.String) playerPhoto = photoEl.GetString();
                    }

                    int goals = 0;
                    string? teamName = null, teamLogo = null;

                    if (item.TryGetProperty("statistics", out var statsArr) &&
                        statsArr.ValueKind == JsonValueKind.Array &&
                        statsArr.GetArrayLength() > 0)
                    {
                        var stat = statsArr[0];
                        if (stat.TryGetProperty("goals", out var goalsEl) &&
                            goalsEl.TryGetProperty("total", out var totalEl) &&
                            totalEl.ValueKind == JsonValueKind.Number)
                            goals = totalEl.GetInt32();

                        if (stat.TryGetProperty("team", out var teamEl))
                        {
                            if (teamEl.TryGetProperty("name", out var tnEl) && tnEl.ValueKind == JsonValueKind.String) teamName = tnEl.GetString();
                            if (teamEl.TryGetProperty("logo", out var tlEl) && tlEl.ValueKind == JsonValueKind.String) teamLogo = tlEl.GetString();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(playerName))
                        list.Add(new GoalKingDto { PlayerId = playerId, Name = playerName, Photo = playerPhoto, Team = teamName, TeamLogo = teamLogo, Goals = goals });
                }
                catch (Exception exItem)
                {
                    _logger.LogWarning(exItem, "Gol krallari item parse hatasi, atlaniyor.");
                }
            }

            _goalKingsCache[cacheKey] = (list, DateTime.UtcNow.Add(GoalKingsCacheDuration));
            _logger.LogInformation("Gol krallari cekildi: leagueId={LeagueId}, season={Season} — {Count} oyuncu", leagueId, season, list.Count);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gol krallari cekilirken hata: leagueId={LeagueId}", leagueId);
            return _goalKingsCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }
}
