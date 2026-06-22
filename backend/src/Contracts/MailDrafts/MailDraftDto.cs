namespace Oypa.Crm.Contracts.MailDrafts;

public sealed record MailDraftDto(
    Guid Id,
    string To,
    string? Cc,
    string Subject,
    string Body,
    bool Sent,
    DateTime? SentAtUtc,
    DateTime CreatedAtUtc,
    Guid? MeetingId);
