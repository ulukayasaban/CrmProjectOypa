namespace Oypa.Crm.Contracts.Employees;

/// <summary>Yönetici atama isteği. ManagerId null ise kök düğüm yapılır.</summary>
public sealed record AssignManagerRequest(Guid? ManagerId);
