using Microsoft.EntityFrameworkCore;
using SkorTakip.API.Data;
using SkorTakip.API.Models;

namespace SkorTakip.API.Services;

/// <summary>
/// Maç bitiminden 1 gün sonra (27 saat tampon dahil) o maça ait yorumları siler.
/// Her saat çalışır.
/// </summary>
public class ChatCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChatCleanupService> _logger;

    private static readonly TimeSpan _interval = TimeSpan.FromHours(1);
    // 1 gün + 3 saatlik maç süresi tamponu
    private static readonly TimeSpan _retentionWindow = TimeSpan.FromHours(27);

    public ChatCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ChatCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ChatCleanupService başladı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatCleanupService temizleme hatası.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope   = _scopeFactory.CreateScope();
        var context       = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cutoff = DateTime.UtcNow - _retentionWindow;

        // Bitmiş ve başlangıç zamanı cutoff'tan önce olan maçların ID'leri
        var expiredMatchIds = await context.Matches
            .Where(m => m.Status == MatchStatus.Finished && m.StartTime < cutoff)
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (expiredMatchIds.Count == 0) return;

        var deleted = await context.MatchComments
            .Where(c => expiredMatchIds.Contains(c.MatchId))
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
        {
            _logger.LogInformation(
                "ChatCleanupService: {Count} yorum silindi ({Matches} maç).",
                deleted, expiredMatchIds.Count);
        }
    }
}
