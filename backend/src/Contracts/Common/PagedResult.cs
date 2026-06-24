namespace Oypa.Crm.Contracts.Common;

/// <summary>
/// Sayfalanmış sorgu sonuçlarını taşıyan jenerik zarf.
/// Frontend ile birebir uyumlu alan adları: items, page, pageSize, totalCount, totalPages.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    /// <summary>Toplam sayfa sayısı. PageSize 0 veya negatifse 0 döner.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
