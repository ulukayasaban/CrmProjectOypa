namespace Oypa.Crm.Contracts.Employees;

/// <summary>Personel ünvan/ad/e-posta güncelleme isteği.</summary>
public sealed record UpdateEmployeeRequest(
    string Title,
    string? FullName,
    string? Email);
