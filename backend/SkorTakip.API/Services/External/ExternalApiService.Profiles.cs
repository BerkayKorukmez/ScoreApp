using SkorTakip.API.DTOs;
using System.Text.Json;

namespace SkorTakip.API.Services;

public partial class ExternalApiService
{
    // Profil endpoint'leri için string-key cache (takım & oyuncu ID'leri büyük olabilir)
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (object? Data, DateTime ExpiresAt)> _profileCache = new();

    // =========================================================================
    // TAKIM PROFİLİ — /teams?id={id}  +  /players/squads?team={id}
    // =========================================================================

    public async Task<TeamProfileDto?> FetchTeamProfileAsync(int teamId)
    {
        var cacheKey = $"team_profile_{teamId}";
        if (_profileCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow &&
            cached.Data is TeamProfileDto dto)
        {
            _logger.LogInformation("Takim profili CACHE: teamId={TeamId}", teamId);
            return dto;
        }

        try
        {
            _logger.LogInformation("Takim profili cekiliyor: teamId={TeamId}", teamId);

            // Paralel: takım bilgisi + kadro
            var teamReq  = new HttpRequestMessage(HttpMethod.Get, $"https://v3.football.api-sports.io/teams?id={teamId}");
            var squadReq = new HttpRequestMessage(HttpMethod.Get, $"https://v3.football.api-sports.io/players/squads?team={teamId}");
            teamReq.Headers.Add("x-apisports-key",  GetApiKey("Football"));
            squadReq.Headers.Add("x-apisports-key", GetApiKey("Football"));

            var teamTask  = _httpClient.SendAsync(teamReq);
            var squadTask = _httpClient.SendAsync(squadReq);
            await Task.WhenAll(teamTask, squadTask);

            var teamContent  = await teamTask.Result.Content.ReadAsStringAsync();
            var squadContent = await squadTask.Result.Content.ReadAsStringAsync();

            using var teamDoc  = JsonDocument.Parse(teamContent);
            using var squadDoc = JsonDocument.Parse(squadContent);

            if (!teamDoc.RootElement.TryGetProperty("response", out var teamArr) ||
                teamArr.ValueKind != JsonValueKind.Array || teamArr.GetArrayLength() == 0)
            {
                _logger.LogWarning("Takim bulunamadi: teamId={TeamId}", teamId);
                return null;
            }

            var item    = teamArr[0];
            var teamEl  = item.GetProperty("team");
            var venueEl = item.TryGetProperty("venue", out var v) ? v : (JsonElement?)null;

            var profile = new TeamProfileDto
            {
                Id       = teamEl.TryGetProperty("id",       out var idEl)   && idEl.ValueKind   == JsonValueKind.Number  ? idEl.GetInt32()      : teamId,
                Name     = teamEl.TryGetProperty("name",     out var nmEl)   && nmEl.ValueKind   == JsonValueKind.String  ? nmEl.GetString()  ?? "" : "",
                Code     = teamEl.TryGetProperty("code",     out var cdEl)   && cdEl.ValueKind   == JsonValueKind.String  ? cdEl.GetString()         : null,
                Country  = teamEl.TryGetProperty("country",  out var cnEl)   && cnEl.ValueKind   == JsonValueKind.String  ? cnEl.GetString()         : null,
                Founded  = teamEl.TryGetProperty("founded",  out var fdEl)   && fdEl.ValueKind   == JsonValueKind.Number  ? fdEl.GetInt32()           : null,
                National = teamEl.TryGetProperty("national", out var natEl)  && natEl.ValueKind  == JsonValueKind.True,
                Logo     = teamEl.TryGetProperty("logo",     out var lgEl)   && lgEl.ValueKind   == JsonValueKind.String  ? lgEl.GetString()         : null,
                Venue    = venueEl.HasValue ? ParseVenue(venueEl.Value) : null,
            };

            // Kadro
            if (squadDoc.RootElement.TryGetProperty("response", out var squadArr) &&
                squadArr.ValueKind == JsonValueKind.Array && squadArr.GetArrayLength() > 0)
            {
                var squadItem = squadArr[0];
                if (squadItem.TryGetProperty("players", out var playersArr) && playersArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in playersArr.EnumerateArray())
                    {
                        try
                        {
                            profile.Squad.Add(new SquadPlayerDto
                            {
                                Id       = p.TryGetProperty("id",       out var pidEl)  && pidEl.ValueKind  == JsonValueKind.Number  ? pidEl.GetInt32()      : 0,
                                Name     = p.TryGetProperty("name",     out var pnmEl)  && pnmEl.ValueKind  == JsonValueKind.String  ? pnmEl.GetString() ?? "" : "",
                                Age      = p.TryGetProperty("age",      out var ageEl)  && ageEl.ValueKind  == JsonValueKind.Number  ? ageEl.GetInt32()      : null,
                                Number   = p.TryGetProperty("number",   out var numEl)  && numEl.ValueKind  == JsonValueKind.Number  ? numEl.GetInt32()      : null,
                                Position = p.TryGetProperty("position", out var posEl)  && posEl.ValueKind  == JsonValueKind.String  ? posEl.GetString()     : null,
                                Photo    = p.TryGetProperty("photo",    out var phEl)   && phEl.ValueKind   == JsonValueKind.String  ? phEl.GetString()      : null,
                            });
                        }
                        catch { /* hatalı oyuncuyu atla */ }
                    }
                }
            }

            _profileCache[cacheKey] = (profile, DateTime.UtcNow.AddMinutes(60));
            _logger.LogInformation("Takim profili cekildi: {Name}, {Count} oyuncu", profile.Name, profile.Squad.Count);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Takim profili hatasi: teamId={TeamId}", teamId);
            return null;
        }
    }

    private static VenueDto ParseVenue(JsonElement v) => new()
    {
        Id       = v.TryGetProperty("id",       out var idEl)   && idEl.ValueKind   == JsonValueKind.Number  ? idEl.GetInt32()      : null,
        Name     = v.TryGetProperty("name",     out var nmEl)   && nmEl.ValueKind   == JsonValueKind.String  ? nmEl.GetString()     : null,
        Address  = v.TryGetProperty("address",  out var adEl)   && adEl.ValueKind   == JsonValueKind.String  ? adEl.GetString()     : null,
        City     = v.TryGetProperty("city",     out var ctEl)   && ctEl.ValueKind   == JsonValueKind.String  ? ctEl.GetString()     : null,
        Capacity = v.TryGetProperty("capacity", out var capEl)  && capEl.ValueKind  == JsonValueKind.Number  ? capEl.GetInt32()     : null,
        Surface  = v.TryGetProperty("surface",  out var sfEl)   && sfEl.ValueKind   == JsonValueKind.String  ? sfEl.GetString()     : null,
        Image    = v.TryGetProperty("image",    out var imgEl)  && imgEl.ValueKind  == JsonValueKind.String  ? imgEl.GetString()    : null,
    };

    // =========================================================================
    // OYUNCU PROFİLİ — /players?id={id}&season={season}
    // =========================================================================

    public async Task<PlayerProfileDto?> FetchPlayerProfileAsync(int playerId, int season)
    {
        var cacheKey = $"player_profile_{playerId}_{season}";
        if (_profileCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow &&
            cached.Data is PlayerProfileDto dto)
        {
            _logger.LogInformation("Oyuncu profili CACHE: playerId={PlayerId}", playerId);
            return dto;
        }

        try
        {
            _logger.LogInformation("Oyuncu profili cekiliyor: playerId={PlayerId}, season={Season}", playerId, season);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://v3.football.api-sports.io/players?id={playerId}&season={season}");
            request.Headers.Add("x-apisports-key", GetApiKey("Football"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            if (!doc.RootElement.TryGetProperty("response", out var arr) ||
                arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
            {
                _logger.LogWarning("Oyuncu bulunamadi: playerId={PlayerId}", playerId);
                return null;
            }

            var item     = arr[0];
            var playerEl = item.GetProperty("player");

            var birth = playerEl.TryGetProperty("birth", out var birthEl) ? birthEl : (JsonElement?)null;

            var profile = new PlayerProfileDto
            {
                Id           = playerEl.TryGetProperty("id",          out var idEl)  && idEl.ValueKind  == JsonValueKind.Number ? idEl.GetInt32()      : playerId,
                Name         = playerEl.TryGetProperty("name",        out var nmEl)  && nmEl.ValueKind  == JsonValueKind.String ? nmEl.GetString() ?? "" : "",
                FirstName    = playerEl.TryGetProperty("firstname",   out var fnEl)  && fnEl.ValueKind  == JsonValueKind.String ? fnEl.GetString()  : null,
                LastName     = playerEl.TryGetProperty("lastname",    out var lnEl)  && lnEl.ValueKind  == JsonValueKind.String ? lnEl.GetString()  : null,
                Age          = playerEl.TryGetProperty("age",         out var ageEl) && ageEl.ValueKind == JsonValueKind.Number ? ageEl.GetInt32()      : null,
                Nationality  = playerEl.TryGetProperty("nationality", out var natEl) && natEl.ValueKind == JsonValueKind.String ? natEl.GetString() : null,
                Height       = playerEl.TryGetProperty("height",      out var htEl)  && htEl.ValueKind  == JsonValueKind.String ? htEl.GetString()  : null,
                Weight       = playerEl.TryGetProperty("weight",      out var wtEl)  && wtEl.ValueKind  == JsonValueKind.String ? wtEl.GetString()  : null,
                Injured      = playerEl.TryGetProperty("injured",     out var injEl) && injEl.ValueKind == JsonValueKind.True,
                Photo        = playerEl.TryGetProperty("photo",       out var phEl)  && phEl.ValueKind  == JsonValueKind.String ? phEl.GetString()  : null,
                BirthDate    = birth.HasValue && birth.Value.TryGetProperty("date",    out var bdEl) && bdEl.ValueKind == JsonValueKind.String ? bdEl.GetString() : null,
                BirthPlace   = birth.HasValue && birth.Value.TryGetProperty("place",   out var bpEl) && bpEl.ValueKind == JsonValueKind.String ? bpEl.GetString() : null,
                BirthCountry = birth.HasValue && birth.Value.TryGetProperty("country", out var bcEl) && bcEl.ValueKind == JsonValueKind.String ? bcEl.GetString() : null,
            };

            if (item.TryGetProperty("statistics", out var statsArr) && statsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var stat in statsArr.EnumerateArray())
                {
                    try
                    {
                        var teamEl   = stat.TryGetProperty("team",   out var t) ? t : (JsonElement?)null;
                        var leagueEl = stat.TryGetProperty("league", out var l) ? l : (JsonElement?)null;
                        var gamesEl  = stat.TryGetProperty("games",  out var g) ? g : (JsonElement?)null;
                        var goalsEl  = stat.TryGetProperty("goals",  out var gl) ? gl : (JsonElement?)null;
                        var cardsEl  = stat.TryGetProperty("cards",  out var c) ? c : (JsonElement?)null;
                        var shotsEl  = stat.TryGetProperty("shots",  out var sh) ? sh : (JsonElement?)null;
                        var dribEl   = stat.TryGetProperty("dribbles", out var dr) ? dr : (JsonElement?)null;

                        int? GetInt(JsonElement? el, string key) =>
                            el.HasValue && el.Value.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;
                        string? GetStr(JsonElement? el, string key) =>
                            el.HasValue && el.Value.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

                        profile.Stats.Add(new PlayerSeasonStatsDto
                        {
                            TeamId          = GetInt(teamEl,   "id"),
                            TeamName        = GetStr(teamEl,   "name"),
                            TeamLogo        = GetStr(teamEl,   "logo"),
                            LeagueId        = GetInt(leagueEl, "id"),
                            LeagueName      = GetStr(leagueEl, "name"),
                            LeagueLogo      = GetStr(leagueEl, "logo"),
                            LeagueFlag      = GetStr(leagueEl, "flag"),
                            Season          = GetInt(leagueEl, "season"),
                            Position        = GetStr(gamesEl,  "position"),
                            Appearances     = GetInt(gamesEl,  "appearences"),
                            Lineups         = GetInt(gamesEl,  "lineups"),
                            Minutes         = GetInt(gamesEl,  "minutes"),
                            Rating          = GetStr(gamesEl,  "rating"),
                            Goals           = GetInt(goalsEl,  "total"),
                            Assists         = GetInt(goalsEl,  "assists"),
                            YellowCards     = GetInt(cardsEl,  "yellow"),
                            RedCards        = GetInt(cardsEl,  "red"),
                            ShotsTotal      = GetInt(shotsEl,  "total"),
                            ShotsOn         = GetInt(shotsEl,  "on"),
                            DribblesAttempts = GetInt(dribEl,  "attempts"),
                            DribblesSuccess  = GetInt(dribEl,  "success"),
                        });
                    }
                    catch { /* hatalı istatistiği atla */ }
                }
            }

            _profileCache[cacheKey] = (profile, DateTime.UtcNow.AddMinutes(60));
            _logger.LogInformation("Oyuncu profili cekildi: {Name}, {Count} stat", profile.Name, profile.Stats.Count);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oyuncu profili hatasi: playerId={PlayerId}", playerId);
            return null;
        }
    }
}
