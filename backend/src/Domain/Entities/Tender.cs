using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Domain.Entities;

/// <summary>
/// İhale agregası. Bir firmaya (Company) bağlı, atanan satış temsilcisinin (SalesRep) sorumluluğundadır.
/// Sektör ayrımı mevcut <see cref="Sector"/> enum ile yapılır.
/// </summary>
public class Tender : BaseEntity
{
    // EF Core için
    private Tender() { }

    private Tender(
        Guid companyId,
        string title,
        string? tenderNumber,
        Sector sector,
        DateOnly tenderDate,
        int? personnelCount,
        decimal? estimatedValue,
        decimal? volume,
        int? quantity,
        string? description,
        Guid? assignedSalesRepId)
    {
        CompanyId = companyId;
        Title = title;
        TenderNumber = tenderNumber;
        Sector = sector;
        TenderDate = tenderDate;
        Status = TenderStatus.Hazirlik;
        PersonnelCount = personnelCount;
        EstimatedValue = estimatedValue;
        Volume = volume;
        Quantity = quantity;
        Description = description;
        AssignedSalesRepId = assignedSalesRepId;
    }

    public Guid CompanyId { get; private set; }
    public Company? Company { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string? TenderNumber { get; private set; }

    /// <summary>İhalenin ait olduğu iş kolu.</summary>
    public Sector Sector { get; private set; }

    /// <summary>Teklifin son verilme ya da ihale kapanış tarihi.</summary>
    public DateOnly TenderDate { get; private set; }

    public TenderStatus Status { get; private set; }

    public int? PersonnelCount { get; private set; }

    /// <summary>Tahmini değer (₺). Negatif değer kabul edilmez.</summary>
    public decimal? EstimatedValue { get; private set; }

    /// <summary>İş hacmi. Negatif değer kabul edilmez.</summary>
    public decimal? Volume { get; private set; }

    public int? Quantity { get; private set; }
    public string? Description { get; private set; }

    /// <summary>Sorumlu satış temsilcisi. Null ise henüz atanmamıştır.</summary>
    public Guid? AssignedSalesRepId { get; private set; }
    public SalesRep? AssignedSalesRep { get; private set; }

    /// <summary>7 gün öncesi bildiriminin gönderildiği UTC zaman damgası (idempotency için).</summary>
    public DateTime? ApproachNotifiedAtUtc { get; private set; }

    // ────────── Factory ──────────

    /// <summary>Yeni bir ihale oluşturur; validasyon yapılır.</summary>
    public static Tender Create(
        Guid companyId,
        string title,
        string? tenderNumber,
        Sector sector,
        DateOnly tenderDate,
        int? personnelCount,
        decimal? estimatedValue,
        decimal? volume,
        int? quantity,
        string? description,
        Guid? assignedSalesRepId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("İhale başlığı boş olamaz.", nameof(title));

        ValidateNumerics(personnelCount, estimatedValue, volume, quantity);

        return new Tender(
            companyId, title, tenderNumber, sector, tenderDate,
            personnelCount, estimatedValue, volume, quantity,
            description, assignedSalesRepId);
    }

    // ────────── Mutation ──────────

    /// <summary>İhale detaylarını günceller.</summary>
    public void UpdateDetails(
        string title,
        string? tenderNumber,
        Sector sector,
        DateOnly tenderDate,
        int? personnelCount,
        decimal? estimatedValue,
        decimal? volume,
        int? quantity,
        string? description,
        Guid? assignedSalesRepId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("İhale başlığı boş olamaz.", nameof(title));

        ValidateNumerics(personnelCount, estimatedValue, volume, quantity);

        Title = title;
        TenderNumber = tenderNumber;
        Sector = sector;
        TenderDate = tenderDate;
        PersonnelCount = personnelCount;
        EstimatedValue = estimatedValue;
        Volume = volume;
        Quantity = quantity;
        Description = description;
        AssignedSalesRepId = assignedSalesRepId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>İhale durumunu değiştirir.</summary>
    public void ChangeStatus(TenderStatus newStatus)
    {
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Yaklaşan-ihale bildiriminin gönderildiğini işaretler (idempotency).</summary>
    public void MarkApproachNotified(DateTime utcNow)
    {
        ApproachNotifiedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    // ────────── Private helpers ──────────

    private static void ValidateNumerics(int? personnelCount, decimal? estimatedValue, decimal? volume, int? quantity)
    {
        if (personnelCount is < 0)
            throw new ArgumentException("Personel sayısı negatif olamaz.", nameof(personnelCount));
        if (estimatedValue is < 0)
            throw new ArgumentException("Tahmini değer negatif olamaz.", nameof(estimatedValue));
        if (volume is < 0)
            throw new ArgumentException("Hacim negatif olamaz.", nameof(volume));
        if (quantity is < 0)
            throw new ArgumentException("Miktar negatif olamaz.", nameof(quantity));
    }
}
