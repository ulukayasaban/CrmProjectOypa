using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Domain.Events;

namespace Oypa.Crm.Domain.Entities;

/// <summary>
/// Firma agregası. Hem lead hem müşteri yaşam döngüsünü tek varlıkta taşır
/// (<see cref="CompanyType"/>), böylece dönüşümde veri tekrarı olmaz.
/// </summary>
public class Company : BaseEntity
{
    private readonly List<Contact> _contacts = [];
    private readonly List<Meeting> _meetings = [];

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
        CompanySource? source = null)
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

    public CompanyType Type { get; private set; }
    public LeadStatus? LeadStatus { get; private set; }
    public CustomerStatus? CustomerStatus { get; private set; }

    /// <summary>Müşteriye dönüştürülme zamanı (yalnızca müşteriler için).</summary>
    public DateTime? ActivatedAtUtc { get; private set; }

    /// <summary>Atanan satış temsilcisinin kimliği. Null ise firma havuzdadır.</summary>
    public Guid? AssignedSalesRepId { get; private set; }

    /// <summary>Atanan satış temsilcisi navigasyon özelliği.</summary>
    public SalesRep? AssignedSalesRep { get; private set; }

    public IReadOnlyCollection<Contact> Contacts => _contacts.AsReadOnly();
    public IReadOnlyCollection<Meeting> Meetings => _meetings.AsReadOnly();

    public void UpdateDetails(
        string title,
        Sector sector,
        string phone,
        string email,
        string address,
        string? city = null,
        string? website = null,
        string? taxNumber = null,
        CompanySource? source = null)
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
