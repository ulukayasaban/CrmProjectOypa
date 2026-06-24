using Oypa.Crm.Application.Common.Interfaces;

namespace Oypa.Crm.Api.BackgroundServices;

/// <summary>
/// Günde bir kez 30 günden eski süresi dolmuş RefreshToken'ları veritabanından temizler.
/// Temizleme mantığı <see cref="IRefreshTokenRepository.DeleteExpiredAsync"/> içinde tutulur;
/// bu sınıf yalnızca zamanlayıcı rolünü üstlenir.
/// </summary>
public sealed class RefreshTokenCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Period = TimeSpan.FromHours(24);

    /// <summary>Temizlenecek token'ların en fazla kaç gün önce süresi dolmuş olabileceği.</summary>
    private const int RetentionDays = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "RefreshTokenCleanupHostedService başlatıldı. İlk temizleme {Delay} dakika sonra çalışacak.",
            InitialDelay.TotalMinutes);

        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCleanupAsync(stoppingToken);

            try
            {
                await Task.Delay(Period, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("RefreshTokenCleanupHostedService durduruldu.");
    }

    private async Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
            var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

            // 30 günden eski süresi dolmuş token'ları sil (aktif token'lara dokunulmaz).
            var cutoff = clock.UtcNow.AddDays(-RetentionDays);
            var deletedCount = await repository.DeleteExpiredAsync(cutoff, stoppingToken);

            logger.LogInformation(
                "Süresi dolmuş RefreshToken temizleme tamamlandı. Silinen kayıt: {Count}, kesim tarihi: {Cutoff:O}.",
                deletedCount,
                cutoff);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Hata servis döngüsünü durdurmamalı; yalnızca loglanır.
            logger.LogError(ex, "RefreshToken temizleme sırasında beklenmeyen hata oluştu.");
        }
    }
}
