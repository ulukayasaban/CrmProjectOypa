namespace Oypa.Crm.Contracts.Employees;

/// <summary>Mevcut hesaba rol atama isteği (Admin veya Sales).</summary>
public sealed record AssignRoleRequest(string Role);
