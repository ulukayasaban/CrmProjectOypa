using Microsoft.Extensions.Diagnostics.HealthChecks;
using Oypa.Crm.Infrastructure.Persistence;

namespace Oypa.Crm.Api.HealthChecks;

/// <summary>
/// Veritabanı bağlantısını test eden özel sağlık kontrolü.
/// Ek NuGet paketi gerektirmez; AppDbContext üzerinden CanConnectAsync kullanılır.
/// </summary>
public sealed class DbHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Veritabanı bağlantısı başarılı.")
                : HealthCheckResult.Unhealthy("Veritabanına bağlanılamadı.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Veritabanı sağlık kontrolü başarısız.", ex);
        }
    }
}
