namespace Oypa.Crm.Contracts.Common;

/// <summary>
/// Sayfalama + arama + sıralama parametrelerini taşıyan ortak sorgu nesnesi.
/// Controller'da [FromQuery] ile bağlanır; geçersiz değerler normalize edilir.
/// </summary>
public sealed record PagedQuery
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>Sayfa numarası (1 tabanlı). 1'den küçük değer 1'e normalize edilir.</summary>
    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    /// <summary>Sayfa boyutu (1–100 arası). Aralık dışı değer sıkıştırılır.</summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 1 : value > 100 ? 100 : value;
    }

    /// <summary>Serbest metin araması (null = arama yok).</summary>
    public string? Search { get; init; }

    /// <summary>Sıralama alanı adı; bilinmeyen değer varsayılana düşer.</summary>
    public string? SortBy { get; init; }

    /// <summary>Sıralama yönü: "asc" veya "desc" (büyük/küçük harf duyarsız). Varsayılan asc.</summary>
    public string? SortDir { get; init; }

    /// <summary>Sıralama yönü desc mi? Kontrolü merkezi tutar.</summary>
    public bool IsDescending =>
        string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}
