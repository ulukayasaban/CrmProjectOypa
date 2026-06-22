using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Meetings;

public sealed record ScheduleMeetingRequest(
    Guid CompanyId,
    Guid? ContactId,
    Guid SalesRepId,
    DateOnly Date,
    TimeOnly Time,
    string Address,
    MeetingMethod Method);
