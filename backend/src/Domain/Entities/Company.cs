using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Domain.Events;

namespace Oypa.Crm.Domain.Entities;

/// <summary>
/// Firma agregası. Hem lead hem müşteri yaşam döngüsünü tek varlıkta taşır
/// (<see cref="CompanyType"/>), böylece dönüşümde veri tekrarı olmaz.
/// Soft-delete destekler; fiziksel silme yerine <see cref="ISoftDelete.MarkDeleted"/> kullanılır.
/// </summary>
public class Company : BaseEntity, ISoftDelete
{
    private readonly List<Contact> _contacts = [];
    private readonly List<Meeting> _meetings = [];
    private readonly List<Category> _categories = [];

    // EF Core için
    private Company() { }

    public Company(
        string title,
        Sector sector,
        string phone,
        string email,
        string address,
        string? city = null,
        string? website = null,
        string? taxNumber = null,
        CompanySource? source = null,
        ServiceSector? serviceSector = null,
        FirmType firmType = FirmType.DisFirma,
        string? sourceNote = null)
    {
        Title = title;
        Sector = sector;
        Phone = phone;
        Email = email;
        Address = address;
        City = city;
        Website = website;
        TaxNumber = taxNumber;
        Source = source;
        ServiceSector = serviceSector;
        FirmType = firmType;
        SourceNote = sourceNote;
        Type = CompanyType.Lead;
        LeadStatus = Enums.LeadStatus.New;
    }

    public string Title { get; private set; } = string.Empty;
    public Sector Sector { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;

    // Opsiyonel profil alanları
    public string? City { get; private set; }
    public string? Website { get; private set; }
    public string? TaxNumber { get; private set; }
    public CompanySource? Source { get; private set; }

    /// <summary>OYPA'nın bu firmaya sunduğu hizmet sektörü (nullable).</summary>
    public ServiceSector? ServiceSector { get; private set; }

    /// <summary>Firmanın OYAK grubu içi / dışı sınıflandırması. Varsayılan: DisFirma.</summary>
    public FirmType FirmType { get; private set; } = FirmType.DisFirma;

    /// <summary>Kaynak alanına ek serbest not (ör. "Belgin Öner referansı").</summary>
    public string? SourceNote { get; private set; }

    public CompanyType Type { get; private set; }
    public LeadStatus? LeadStatus { get; private set; }
    public CustomerStatus? CustomerStatus { get; private set; }

    /// <summary>Müşteriye dönüştürülme zamanı (yalnızca müşteriler için).</summary>
    public DateTime? ActivatedAtUtc { get; private set; }

    /// <summary>
    /// Firma yeni müşteri bayrağı.
    /// Convert sırasında veya oluşturulurken set edilir; varsayılan false.
    /// </summary>
    public bool IsNewCustomer { get; private set; }

    /// <summary>
    /// Son etkileşim zaman damgası (UTC).
    /// Görüşme, not veya ihale oluşturulduğunda güncellenir.
    /// Null ise henüz etkileşim kaydedilmemiştir.
    /// </summary>
    public DateTime? LastInteractionAtUtc { get; private set; }

    /// <summary>Atanan satış temsilcisinin kimliği. Null ise firma havuzdadır.</summary>
    public Guid? AssignedSalesRepId { get; private set; }

    /// <summary>Atanan satış temsilcisi navigasyon özelliği.</summary>
    public SalesRep? AssignedSalesRep { get; private set; }

    /// <summary>Lead ile iletişim kuran satış temsilcisinin kimliği (AssignedSalesRep'ten bağımsız).</summary>
    public Guid? LeadOwnerId { get; private set; }

    /// <summary>Lead owner navigasyon özelliği.</summary>
    public SalesRep? LeadOwner { get; private set; }

    public IReadOnlyCollection<Contact> Contacts => _contacts.AsReadOnly();
    public IReadOnlyCollection<Meeting> Meetings => _meetings.AsReadOnly();
    public IReadOnlyCollection<Category> Categories => _categories.AsReadOnly();

    // ────────── ISoftDelete ──────────

    /// <summary>Firma silinmiş olarak işaretlenmiş mi.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Silme zaman damgası (UTC). Silinmediyse null.</summary>
    public DateTime? DeletedAtUtc { get; private set; }

    /// <summary>Firmayı mantıksal olarak siler; DB'den fiziksel kaldırılmaz.</summary>
    public void MarkDeleted(DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Silinmiş firmayı geri yükler.</summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string title,
        Sector sector,
        string phone,
        string email,
        string address,
        string? city = null,
        string? website = null,
        string? taxNumber = null,
        CompanySource? source = null,
        ServiceSector? serviceSector = null,
        FirmType firmType = FirmType.DisFirma,
        string? sourceNote = null)
    {
        Title = title;
        Sector = sector;
        Phone = phone;
        Email = email;
        Address = address;
        City = city;
        Website = website;
        TaxNumber = taxNumber;
        Source = source;
        ServiceSector = serviceSector;
        FirmType = firmType;
        SourceNote = sourceNote;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Lead ile iletişim kuran satış temsilcisini atar.
    /// <paramref name="salesRepId"/> null ise mevcut atama kaldırılır.
    /// </summary>
    public void SetLeadOwner(Guid? salesRepId)
    {
        LeadOwnerId = salesRepId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetLeadStatus(LeadStatus status)
    {
        if (Type != CompanyType.Lead)
            throw new InvalidOperationException("Lead durumu yalnızca lead aşamasındaki firmalarda değiştirilebilir.");

        LeadStatus = status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Müşteri segment durumunu günceller. Yalnızca müşteri firmalarda geçerlidir.</summary>
    public void SetCustomerStatus(CustomerStatus status)
    {
        if (Type != CompanyType.Customer)
            throw new InvalidOperationException("Müşteri durumu yalnızca müşteri aşamasındaki firmalarda değiştirilebilir.");

        CustomerStatus = status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Firmaya satış temsilcisi atar. <paramref name="salesRepId"/> null ise firma havuza alınır.
    /// </summary>
    public void AssignSalesRep(Guid? salesRepId)
    {
        AssignedSalesRepId = salesRepId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Firmaya atanacak kategorileri toptan ayarlar. Mevcut kategoriler temizlenerek yenileri eklenir.
    /// </summary>
    public void SetCategories(IEnumerable<Category> categories)
    {
        _categories.Clear();
        _categories.AddRange(categories);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Yeni müşteri bayrağını ayarlar.</summary>
    public void MarkNewCustomer(bool value)
    {
        IsNewCustomer = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Son etkileşim zaman damgasını günceller.
    /// <paramref name="reactivate"/> true ise ve firma pasif müşteriyse Active'e döndürür.
    /// </summary>
    public void RegisterInteraction(DateTime utcNow, bool reactivate)
    {
        LastInteractionAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        if (reactivate && Type == CompanyType.Customer && CustomerStatus == Enums.CustomerStatus.Passive)
        {
            CustomerStatus = Enums.CustomerStatus.Active;
        }
    }

    /// <summary>Lead'i aktif müşteriye dönüştürür.</summary>
    public void ConvertToCustomer()
    {
        if (Type == CompanyType.Customer)
            throw new InvalidOperationException("Firma zaten müşteri.");

        Type = CompanyType.Customer;
        CustomerStatus = Enums.CustomerStatus.Active;
        LeadStatus = null;
        ActivatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new LeadConvertedToCustomerEvent(Id, Title));
    }
}
