using Oypa.Crm.Contracts.Dashboard;

namespace Oypa.Crm.Application.Features.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
