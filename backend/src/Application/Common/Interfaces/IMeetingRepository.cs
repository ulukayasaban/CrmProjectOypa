using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Common.Interfaces;

public interface IMeetingRepository : IRepository<Meeting>
{
    /// <summary>Tüm görüşmeleri Company/SalesRep/Contact ilişkileriyle getirir.</summary>
    Task<IReadOnlyList<Meeting>> ListWithDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>Bir firmaya ait görüşmeleri ilişkileriyle getirir.</summary>
    Task<IReadOnlyList<Meeting>> ListByCompanyWithDetailsAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen temsilci kümesine ait, belirtilen tarih aralığında tamamlanmış (Done)
    /// görüşme sayısını segment filtresiyle döndürür.
    /// </summary>
    Task<int> CountDoneByRepsAndSegmentAsync(
        IReadOnlyCollection<Guid> salesRepIds,
        DateOnly weekStart,
        DateOnly weekEnd,
        GoalSegment segment,
        CancellationToken cancellationToken = default);
}
