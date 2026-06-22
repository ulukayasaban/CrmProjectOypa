using Oypa.Crm.Application.Features.Tenders;

namespace Oypa.Crm.Api.BackgroundServices;

/// <summary>
/// Periyodik olarak yaklaşan ihaleleri tarar ve atanan sorumluya bildirim gönderir.
/// Tarama mantığı test edilebilirlik için <see cref="ITenderReminderService"/>'te tutulur;
/// bu sınıf yalnızca zamanlayıcı rolünü üstlenir.
/// </summary>
public sealed class UpcomingTenderReminderHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<UpcomingTenderReminderHostedService> logger) : BackgroundService
{
    private const int DaysAhead = 7;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Period = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "UpcomingTenderReminderHostedService başlatıldı. İlk tarama {Delay} saniye sonra.",
            InitialDelay.TotalSeconds);

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
            await RunScanAsync(stoppingToken);

            try
            {
                await Task.Delay(Period, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("UpcomingTenderReminderHostedService durduruldu.");
    }

    private async Task RunScanAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<ITenderReminderService>();
            var count = await reminderService.NotifyApproachingAsync(DaysAhead, stoppingToken);

            logger.LogInformation(
                "Yaklaşan ihale taraması tamamlandı. Bildirim gönderilen ihale sayısı: {Count}.",
                count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Hata servis döngüsünü durdurmamalı; yalnızca loglanır.
            logger.LogError(ex, "Yaklaşan ihale taraması sırasında beklenmeyen hata oluştu.");
        }
    }
}
