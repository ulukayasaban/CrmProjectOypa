namespace Oypa.Crm.Contracts.Contacts;

public sealed record CreateContactRequest(
    string Name,
    string? Email,
    string? Phone);
