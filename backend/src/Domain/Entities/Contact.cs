using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Entities;

/// <summary>Bir firmaya bağlı ilgili kişi.</summary>
public class Contact : BaseEntity
{
    private Contact() { }

    public Contact(Guid companyId, string name, string? email, string? phone)
    {
        CompanyId = companyId;
        Name = name;
        Email = email;
        Phone = phone;
    }

    public Guid CompanyId { get; private set; }
    public Company? Company { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    public void Update(string name, string? email, string? phone)
    {
        Name = name;
        Email = email;
        Phone = phone;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
