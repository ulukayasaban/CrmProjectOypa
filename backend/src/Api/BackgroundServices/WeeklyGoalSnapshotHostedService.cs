using Oypa.Crm.Application.Features.Goals;

namespace Oypa.Crm.Api.BackgroundServices;

/// <summary>
/// Her 6 saatte bir tüm aktif hedefler için haftalık snapshot'ları arka planda oluşturur.
/// <para>
/// GoalService.GetScopedAsync / GetWeeksAsync tembel snapshot mantığını bozmaz;
/// bu job, snapshot oluşturmayı isteğe bağlı bir ön çalışmaya öteleyerek
/// istek anındaki yükü ve olası tutarsızlığı azaltır.
/// </para>
/// </summary>
public sealed class WeeklyGoalSnapshotHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<WeeklyGoalSnapshotHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan Period = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "WeeklyGoalSnapshotHostedService başlatıldı. İlk snapshot {Delay} saniye sonra çalışacak.",
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
            await RunSnapshotAsync(stoppingToken);

            try
            {
                await Task.Delay(Period, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("WeeklyGoalSnapshotHostedService durduruldu.");
    }

    private async Task RunSnapshotAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var goalService = scope.ServiceProvider.GetRequiredService<IGoalService>();
            await goalService.SnapshotAllAsync(stoppingToken);

            logger.LogInformation("Haftalık hedef snapshot işlemi tamamlandı.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Hata servis döngüsünü durdurmamalı; yalnızca loglanır.
            logger.LogError(ex, "Haftalık hedef snapshot sırasında beklenmeyen hata oluştu.");
        }
    }
}
