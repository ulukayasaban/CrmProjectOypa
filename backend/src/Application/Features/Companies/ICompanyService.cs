using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Companies;

public interface ICompanyService
{
    Task<IReadOnlyList<CompanyDto>> GetLeadsAsync(LeadStatus? status = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyDto>> GetCustomersAsync(CustomerStatus? status = null, CancellationToken cancellationToken = default);

    Task<CompanyDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);

    Task<CompanyDto> UpdateAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default);

    Task SetLeadStatusAsync(Guid id, LeadStatus status, CancellationToken cancellationToken = default);

    Task SetCustomerStatusAsync(Guid id, CustomerStatus status, CancellationToken cancellationToken = default);

    Task<CompanyDto> ConvertToCustomerAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Firmaya satış temsilcisi atar. <paramref name="salesRepId"/> null ise firma havuza alınır.
    /// </summary>
    Task AssignSalesRepAsync(Guid id, Guid? salesRepId, CancellationToken cancellationToken = default);
}
