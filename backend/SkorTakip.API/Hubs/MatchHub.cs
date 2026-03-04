using Microsoft.AspNetCore.SignalR;
using SkorTakip.API.Services;

namespace SkorTakip.API.Hubs;

public class MatchHub : Hub
{
    private readonly LiveMatchService _liveMatchService;

    public MatchHub(LiveMatchService liveMatchService)
    {
        _liveMatchService = liveMatchService;
    }

    /// <summary>
    /// Yeni bir istemci bağlandığında cache'teki mevcut maçları gönderir.
    /// Böylece sayfa açılır açılmaz maçlar görünür.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var cachedMatches = _liveMatchService.GetCachedMatches().ToList();

        if (cachedMatches.Count > 0)
        {
            await Clients.Caller.SendAsync("AllMatches", cachedMatches);
        }

        await base.OnConnectedAsync();
    }
}
