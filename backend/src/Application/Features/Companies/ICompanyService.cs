using Oypa.Crm.Contracts.Common;
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

    /// <summary>
    /// Lead'i müşteriye dönüştürür.
    /// Opsiyonel olarak satış temsilcisi atar, hizmet sektörü set eder ve yeni müşteri bayrağını işaretler.
    /// </summary>
    Task<CompanyDto> ConvertToCustomerAsync(Guid id, ConvertToCustomerRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Firmaya satış temsilcisi atar. <paramref name="salesRepId"/> null ise firma havuza alınır.
    /// </summary>
    Task AssignSalesRepAsync(Guid id, Guid? salesRepId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lead ile iletişim kuran satış temsilcisini atar.
    /// <paramref name="salesRepId"/> null ise mevcut atama kaldırılır.
    /// </summary>
    Task SetLeadOwnerAsync(Guid id, Guid? salesRepId, CancellationToken cancellationToken = default);

    /// <summary>Firmaya kategorileri toptan atar ve güncel CompanyDto döner.</summary>
    Task<CompanyDto> SetCategoriesAsync(Guid companyId, IReadOnlyList<Guid> categoryIds, CancellationToken cancellationToken = default);

    /// <summary>Lead firmaları sayfalama + arama + sıralama ile listeler.</summary>
    Task<PagedResult<CompanyDto>> GetLeadsPagedAsync(
        LeadStatus? status,
        PagedQuery query,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Müşteri firmaları sayfalama + arama + sıralama ile listeler.</summary>
    Task<PagedResult<CompanyDto>> GetCustomersPagedAsync(
        CustomerStatus? status,
        PagedQuery query,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);
}
