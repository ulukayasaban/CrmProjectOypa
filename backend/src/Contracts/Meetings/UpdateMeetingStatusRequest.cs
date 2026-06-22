using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Meetings;

public sealed record UpdateMeetingStatusRequest(
    MeetingStatus Status,
    string? Comment);
