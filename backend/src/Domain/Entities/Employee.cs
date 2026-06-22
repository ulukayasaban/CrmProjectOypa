using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Entities;

/// <summary>OYPA organizasyon hiyerarşisindeki personel düğümü. Opsiyonel olarak bir kimlik hesabıyla ilişkilendirilebilir.</summary>
public class Employee : BaseEntity
{
    private Employee() { }

    public Employee(string title, string? fullName = null, string? email = null, Guid? managerId = null)
    {
        Title = title;
        FullName = fullName;
        Email = email;
        ManagerId = managerId;
    }

    public string? FullName { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public Guid? ManagerId { get; private set; }

    /// <summary>Yönetici navigasyonu (self-referencing).</summary>
    public Employee? Manager { get; private set; }

    /// <summary>Bağlı kimlik hesabının Id'si. Hesap yoksa null.</summary>
    public Guid? ApplicationUserId { get; private set; }

    /// <summary>Personeli bir kimlik hesabıyla ilişkilendirir.</summary>
    public void LinkAccount(Guid userId)
    {
        ApplicationUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Ünvan, ad ve e-posta bilgilerini günceller.</summary>
    public void UpdateDetails(string title, string? fullName, string? email)
    {
        Title = title;
        FullName = fullName;
        Email = email;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Yöneticiyi atar veya kaldırır (null = kök düğüm).</summary>
    public void AssignManager(Guid? managerId)
    {
        ManagerId = managerId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Kimlik hesabı bağlantısını kaldırır; ApplicationUser'a dokunulmaz.</summary>
    public void UnlinkAccount()
    {
        ApplicationUserId = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
