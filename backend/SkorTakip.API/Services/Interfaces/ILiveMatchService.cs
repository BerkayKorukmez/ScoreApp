using SkorTakip.API.Models;

namespace SkorTakip.API.Services.Interfaces;

public interface ILiveMatchService
{
    IEnumerable<Match> GetCachedMatches();
}
