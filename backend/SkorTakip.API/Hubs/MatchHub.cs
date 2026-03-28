using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Data;
using SkorTakip.API.Services.Interfaces;

namespace SkorTakip.API.Hubs;

public class MatchHub : Hub
{
    private readonly ILiveMatchService _liveMatchService;
    private readonly ApplicationDbContext _context;

    public MatchHub(ILiveMatchService liveMatchService, ApplicationDbContext context)
    {
        _liveMatchService = liveMatchService;
        _context = context;
    }

    /// <summary>
    /// Yeni bir istemci bağlandığında cache'teki mevcut maçları gönderir.
    /// Admin tarafından gizlenen maçlar filtrelenir.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var hiddenIds = await _context.Matches
            .Where(m => m.IsHidden)
            .Select(m => m.Id)
            .ToListAsync();
        var hiddenSet = hiddenIds.ToHashSet();

        var cachedMatches = _liveMatchService.GetCachedMatches()
            .Where(m => !hiddenSet.Contains(m.Id))
            .ToList();

        if (cachedMatches.Count > 0)
        {
            await Clients.Caller.SendAsync("AllMatches", cachedMatches);
        }

        await base.OnConnectedAsync();
    }
}
