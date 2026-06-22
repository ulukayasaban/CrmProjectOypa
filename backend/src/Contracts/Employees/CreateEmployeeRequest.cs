namespace Oypa.Crm.Contracts.Employees;

/// <summary>Yeni personel oluşturma isteği. CreateAccount true ise Email ve Role zorunludur.</summary>
public sealed record CreateEmployeeRequest(
    string Title,
    string? FullName,
    string? Email,
    Guid? ManagerId,
    bool CreateAccount,
    string? Role);
