using System.Collections.Concurrent;
using System.Net;
using SkorTakip.API.DTOs;
using System.Text.Json;

namespace SkorTakip.API.Services;

/// <summary>
/// CollectAPI sport/goalKings — gol krallığı (futbol).
/// Sport API farklı key kullanır: SportCollectApi:ApiKey
/// </summary>
public partial class ExternalApiService
{
    private static readonly ConcurrentDictionary<string, (List<GoalKingDto> Data, DateTime ExpiresAt)> _goalKingsCache = new();
    private static readonly TimeSpan GoalKingsCacheDuration = TimeSpan.FromMinutes(30);

    public async Task<List<GoalKingDto>> FetchGoalKingsFromSportApiAsync(string leagueKey)
    {
        var cacheKey = $"sport_goalkings_{leagueKey}";
        if (_goalKingsCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            _logger.LogInformation("Gol krallari CACHE: {LeagueKey} ({Count})", leagueKey, cached.Data.Count);
            return cached.Data;
        }

        try
        {
            var apiKey = _configuration["SportCollectApi:ApiKey"]
                ?? throw new InvalidOperationException("SportCollectApi:ApiKey configuration is missing.");

            var url = $"https://api.collectapi.com/sport/goalKings?league={Uri.EscapeDataString(leagueKey)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("authorization", $"apikey {apiKey}");

            var response = await _httpClient.SendAsync(req);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Sport API 429 — {LeagueKey}", leagueKey);
                return _goalKingsCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            JsonElement items;

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                items = doc.RootElement;
            else if (doc.RootElement.TryGetProperty("result", out var res) && res.ValueKind == JsonValueKind.Array)
                items = res;
            else if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                items = data;
            else
            {
                _logger.LogWarning("Sport goalKings beklenmeyen format: {LeagueKey} — Raw: {Preview}",
                    leagueKey, content.Length > 500 ? content[..500] + "..." : content);
                return [];
            }

            var list = new List<GoalKingDto>();
            var rank = 0;
            foreach (var item in items.EnumerateArray())
            {
                rank++;
                var name  = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var goals = item.TryGetProperty("goals", out var g)
                    ? (g.ValueKind == JsonValueKind.Number ? g.GetInt32() : int.TryParse(g.GetString(), out var v) ? v : 0)
                    : 0;

                list.Add(new GoalKingDto { Name = name, Goals = goals });
            }

            _goalKingsCache[cacheKey] = (list, DateTime.UtcNow.Add(GoalKingsCacheDuration));
            _logger.LogInformation("Gol krallari cekildi: {LeagueKey} — {Count} oyuncu", leagueKey, list.Count);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sport goalKings hatasi: {LeagueKey}", leagueKey);
            return _goalKingsCache.TryGetValue(cacheKey, out var stale) ? stale.Data : [];
        }
    }
}
