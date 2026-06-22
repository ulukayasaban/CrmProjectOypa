using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Meetings;

public sealed record MeetingDto(
    Guid Id,
    Guid CompanyId,
    string CompanyTitle,
    Guid? ContactId,
    string? ContactName,
    Guid SalesRepId,
    string SalesRepName,
    string? SalesRepTitle,
    DateOnly Date,
    TimeOnly Time,
    string Address,
    MeetingMethod Method,
    MeetingStatus Status,
    string? Comment,
    IReadOnlyList<MeetingNoteDto> Notes);
