using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkorTakip.API.Data;
using SkorTakip.API.Hubs;
using SkorTakip.API.Models;

namespace SkorTakip.API.Services;

public class MatchSimulationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Random _random = new();

    public MatchSimulationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<MatchHub>>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<MatchSimulationService>>();

                var liveMatches = await context.Matches
                    .Where(m => m.Status == MatchStatus.Live)
                    .ToListAsync(stoppingToken);

                foreach (var match in liveMatches)
                {
                    // Maç dakikasını artır
                    match.Minute += 1;

                    // 45. dakikada yarı devre
                    if (match.Minute == 45)
                    {
                        match.Status = MatchStatus.HalfTime;
                    }
                    // 46. dakikada ikinci yarı başlar
                    else if (match.Minute == 46)
                    {
                        match.Status = MatchStatus.Live;
                    }
                    // 90. dakikada maç biter
                    else if (match.Minute >= 90)
                    {
                        match.Status = MatchStatus.Finished;
                    }
                    // Canlı maçlarda rastgele gol atma ihtimali
                    else if (match.Status == MatchStatus.Live && match.Minute < 90)
                    {
                        // Her dakika %15 ihtimalle gol atılabilir
                        if (_random.Next(100) < 15)
                        {
                            // Hangi takım gol atacak? (50-50)
                            if (_random.Next(2) == 0)
                            {
                                match.HomeScore++;
                            }
                            else
                            {
                                match.AwayScore++;
                            }
                        }
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    await hubContext.Clients.All.SendAsync("MatchUpdated", match);
                }

                // Her 10 saniyede bir güncelle
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (Exception ex)
            {
                using var errorScope = _serviceProvider.CreateScope();
                var logger = errorScope.ServiceProvider.GetRequiredService<ILogger<MatchSimulationService>>();
                logger.LogError(ex, "Match simulation error: {Message}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
