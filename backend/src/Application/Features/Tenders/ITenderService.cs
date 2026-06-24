using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Tenders;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Tenders;

/// <summary>İhale CRUD ve durum yönetimi servisi.</summary>
public interface ITenderService
{
    Task<IReadOnlyList<TenderDto>> GetAsync(Sector? sector, TenderStatus? status, CancellationToken cancellationToken = default);

    Task<TenderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TenderDto> CreateAsync(CreateTenderRequest request, CancellationToken cancellationToken = default);

    Task<TenderDto> UpdateAsync(Guid id, UpdateTenderRequest request, CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(Guid id, ChangeTenderStatusRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// İhaleleri sayfalama + arama + sıralama ile listeler.
    /// Mevcut sektör/durum filtreleri ek parametre olarak korunur.
    /// </summary>
    Task<PagedResult<TenderDto>> GetPagedAsync(
        Sector? sector,
        TenderStatus? status,
        IReadOnlyCollection<TenderStatus>? statuses,
        PagedQuery query,
        CancellationToken cancellationToken = default);
}
