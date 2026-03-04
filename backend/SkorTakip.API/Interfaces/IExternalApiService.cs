using SkorTakip.API.Models;

namespace SkorTakip.API.Interfaces;

public interface IExternalApiService
{
    Task<List<Match>> FetchFootballMatchesAsync();
    Task<List<Match>> FetchBasketballMatchesAsync();
    Task<List<Match>> FetchVolleyballMatchesAsync();
    Task<List<Match>> FetchTennisMatchesAsync();
    Task<Dictionary<string, object>?> FetchFootballMatchStatisticsAsync(int fixtureId);
}
