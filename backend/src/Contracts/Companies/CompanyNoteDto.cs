namespace Oypa.Crm.Contracts.Companies;

public sealed record CompanyNoteDto(
    Guid Id,
    string Content,
    string AuthorName,
    DateTime CreatedAtUtc);
