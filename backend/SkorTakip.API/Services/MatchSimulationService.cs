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
    private readonly ILogger<MatchSimulationService> _logger;
    private readonly Random _random = new();

    public MatchSimulationService(IServiceProvider serviceProvider, ILogger<MatchSimulationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MatchSimulationService baslatildi.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<MatchHub>>();

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
                    await hubContext.Clients.All.SendAsync("MatchUpdated", match, stoppingToken);
                }

                // Her 10 saniyede bir güncelle
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Uygulama kapanıyor, normal çıkış
                _logger.LogInformation("MatchSimulationService durduruluyor.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Match simulation hatasi: {Message}", ex.Message);

                // İptal edilmediyse bekle ve tekrar dene
                if (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
    }
}
