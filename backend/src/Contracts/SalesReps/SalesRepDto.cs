namespace Oypa.Crm.Contracts.SalesReps;

public sealed record SalesRepDto(
    Guid Id,
    string Name,
    string Email,
    Guid? EmployeeId);
