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

    /// <summary>
    /// Belirtilen maçın sohbet grubuna katılır ve son yorumları gönderir.
    /// </summary>
    public async Task JoinMatchChat(string matchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"match-{matchId}");

        var recentComments = await _context.MatchComments
            .Where(c => c.MatchId == matchId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(50)
            .OrderBy(c => c.CreatedAt)
            .Include(c => c.User)
            .Select(c => new
            {
                id        = c.Id,
                matchId   = c.MatchId,
                userId    = c.UserId,
                userName  = c.User.UserName,
                content   = c.Content,
                createdAt = c.CreatedAt
            })
            .ToListAsync();

        await Clients.Caller.SendAsync("RecentComments", recentComments);
    }

    /// <summary>
    /// Belirtilen maçın sohbet grubundan ayrılır.
    /// </summary>
    public async Task LeaveMatchChat(string matchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"match-{matchId}");
    }
}
