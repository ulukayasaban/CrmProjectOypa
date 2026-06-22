namespace Oypa.Crm.Contracts.Meetings;

public sealed record MeetingNoteDto(
    Guid Id,
    string Content,
    string AuthorName,
    string? AuthorTitle,
    DateTime CreatedAtUtc);
