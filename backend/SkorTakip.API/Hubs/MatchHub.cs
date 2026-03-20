using Microsoft.AspNetCore.SignalR;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Hubs;

public class MatchHub : Hub
{
    private readonly ILiveMatchService _liveMatchService;

    public MatchHub(ILiveMatchService liveMatchService)
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
