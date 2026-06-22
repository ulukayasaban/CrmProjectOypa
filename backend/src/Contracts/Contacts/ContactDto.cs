namespace Oypa.Crm.Contracts.Contacts;

public sealed record ContactDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string? Email,
    string? Phone);
