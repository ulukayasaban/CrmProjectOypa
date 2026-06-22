using Oypa.Crm.Contracts.SalesReps;

namespace Oypa.Crm.Application.Features.SalesReps;

public interface ISalesRepService
{
    Task<IReadOnlyList<SalesRepDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SalesRepDto> CreateAsync(CreateSalesRepRequest request, CancellationToken cancellationToken = default);

    Task<SalesRepDto> LinkEmployeeAsync(Guid id, Guid? employeeId, CancellationToken cancellationToken = default);
}
