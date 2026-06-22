using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Common.Interfaces;

public interface ITenderRepository : IRepository<Tender>
{
    /// <summary>İhaleleri isteğe bağlı sektör/durum filtresiyle, Company ve AssignedSalesRep ilişkileriyle getirir.</summary>
    Task<IReadOnlyList<Tender>> ListAsync(
        Sector? sector,
        TenderStatus? status,
        CancellationToken cancellationToken = default);

    /// <summary>Tek bir ihaleyi tüm detay ilişkileriyle (Company, AssignedSalesRep) getirir.</summary>
    Task<Tender?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bildirim gönderilecek yaklaşan ihaleleri döndürür:
    /// durum Hazirlik veya TeklifVerildi, ApproachNotifiedAtUtc null,
    /// TenderDate ∈ [today, today+daysAhead], AssignedSalesRepId not null.
    /// AssignedSalesRep.Employee ilişkisiyle yüklenir.
    /// </summary>
    Task<IReadOnlyList<Tender>> ListApproachingAsync(
        DateOnly today,
        int daysAhead,
        CancellationToken cancellationToken = default);
}
