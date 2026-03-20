using SkorTakip.API.DTOs;
using SkorTakip.API.Models;

namespace SkorTakip.API.Services.Interfaces;

public interface IExternalApiService
{
    // ── Günlük maçlar ────────────────────────────────────────────────────────────
    Task<List<Match>> FetchFootballMatchesAsync();
    Task<List<Match>> FetchBasketballMatchesAsync();
    Task<List<Match>> FetchVolleyballMatchesAsync();
    Task<List<Match>> FetchTennisMatchesAsync();

    // ── Maç istatistikleri / olaylar ─────────────────────────────────────────────
    Task<Dictionary<string, object>?> FetchFootballMatchStatisticsAsync(int fixtureId);
    Task<Dictionary<string, object>?> FetchBasketballMatchStatisticsAsync(int gameId);
    Task<Dictionary<string, object>?> FetchVolleyballMatchStatisticsAsync(int gameId);
    Task<List<Dictionary<string, object>>?> FetchFootballMatchEventsAsync(int fixtureId);

    // ── Son hafta maç sonuçları (CollectAPI) ──────────────────────────────────────
    Task<List<MatchResultDto>> FetchFootballResultsFromCollectApiAsync(string leagueKey, string? date = null);

    // ── Puan durumu ───────────────────────────────────────────────────────────────
    Task<List<LeagueStandingDto>> FetchFootballStandingsAsync(int leagueId, int season);
    Task<List<LeagueStandingDto>> FetchFootballStandingsFromCollectApiAsync(string leagueKey);
    Task<List<LeagueStandingDto>> FetchBasketballStandingsAsync(int leagueId, string season);
    Task<List<LeagueStandingDto>> FetchVolleyballStandingsAsync(int leagueId, int season);

    // ── Tarih bazlı geçmiş maçlar ─────────────────────────────────────────────────
    Task<List<Match>> FetchFootballMatchesByDateAsync(string date);
    Task<List<Match>> FetchBasketballMatchesByDateAsync(string date);
    Task<List<Match>> FetchVolleyballMatchesByDateAsync(string date);

    // ── Fikstür: takım/lig arama ─────────────────────────────────────────────────
    Task<List<TeamSearchResultDto>> SearchFootballTeamsAsync(string query);
    Task<List<TeamSearchResultDto>> SearchBasketballTeamsAsync(string query);
    Task<List<TeamSearchResultDto>> SearchVolleyballTeamsAsync(string query);
    Task<List<TeamSearchResultDto>> SearchFootballLeaguesAsync(string query);
    Task<List<TeamSearchResultDto>> SearchBasketballLeaguesAsync(string query);
    Task<List<TeamSearchResultDto>> SearchVolleyballLeaguesAsync(string query);

    // ── Fikstür: takıma göre ─────────────────────────────────────────────────────
    Task<List<Match>> FetchFootballFixturesByTeamAsync(int teamId, int season);
    Task<List<Match>> FetchBasketballFixturesByTeamAsync(int teamId, string season);
    Task<List<Match>> FetchVolleyballFixturesByTeamAsync(int teamId, int season);

    // ── Fikstür: lige göre ───────────────────────────────────────────────────────
    Task<List<Match>> FetchFootballFixturesByLeagueAsync(int leagueId, int season);
    Task<List<Match>> FetchBasketballFixturesByLeagueAsync(int leagueId, string season);
    Task<List<Match>> FetchVolleyballFixturesByLeagueAsync(int leagueId, int season);
}
