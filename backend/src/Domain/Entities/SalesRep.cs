using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Entities;

/// <summary>OYPA satış temsilcisi (görüşmelere atanır).</summary>
public class SalesRep : BaseEntity
{
    private SalesRep() { }

    public SalesRep(string name, string email)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    /// <summary>Bağlı org personeli (isteğe bağlı); hedef hesabında kullanılır.</summary>
    public Guid? EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public void Update(string name, string email)
    {
        Name = name;
        Email = email;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Temsilciyi bir org düğümüne bağlar veya bağı koparır (null).</summary>
    public void LinkEmployee(Guid? employeeId)
    {
        EmployeeId = employeeId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
