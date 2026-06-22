namespace Oypa.Crm.Contracts.Employees;

/// <summary>Organizasyon hiyerarşisi için personel yanıt DTO'su.</summary>
public record EmployeeDto(
    Guid Id,
    string? FullName,
    string Title,
    string? Email,
    Guid? ManagerId,
    string? ManagerName,
    bool HasAccount,
    string? Role);
