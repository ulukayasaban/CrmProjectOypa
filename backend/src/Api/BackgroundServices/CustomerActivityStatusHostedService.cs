using Oypa.Crm.Application.Features.Companies;

namespace Oypa.Crm.Api.BackgroundServices;

/// <summary>
/// Periyodik olarak aktif müşterileri tarar; son etkileşim tarihi 6 aydan eskiyse pasife alır.
/// Tarama mantığı test edilebilirlik için <see cref="ICustomerActivityService"/>'te tutulur;
/// bu sınıf yalnızca zamanlayıcı rolünü üstlenir.
/// Varsayılan interval: günde bir. Yapılandırma: "CustomerActivityHostedService:IntervalHours".
/// </summary>
public sealed class CustomerActivityStatusHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<CustomerActivityStatusHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(45);

    private TimeSpan Period
    {
        get
        {
            var hours = configuration.GetValue<double>("CustomerActivityHostedService:IntervalHours", 24.0);
            return TimeSpan.FromHours(hours);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "CustomerActivityStatusHostedService başlatıldı. İlk tarama {Delay} saniye sonra.",
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

        logger.LogInformation("CustomerActivityStatusHostedService durduruldu.");
    }

    private async Task RunScanAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var activityService = scope.ServiceProvider.GetRequiredService<ICustomerActivityService>();
            var count = await activityService.DeactivateInactiveCustomersAsync(stoppingToken);

            logger.LogInformation(
                "Müşteri aktivite taraması tamamlandı. Pasife alınan müşteri sayısı: {Count}.",
                count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Hata servis döngüsünü durdurmamalı; yalnızca loglanır.
            logger.LogError(ex, "Müşteri aktivite taraması sırasında beklenmeyen hata oluştu.");
        }
    }
}
