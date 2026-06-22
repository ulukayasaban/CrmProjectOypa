namespace Oypa.Crm.Contracts.Employees;

/// <summary>Personel oluşturma yanıtı. Hesap kurulduysa Account alanı dolu gelir (UI bir kez gösterir).</summary>
public sealed record CreateEmployeeResult(EmployeeDto Employee, AccountCredentialDto? Account);
