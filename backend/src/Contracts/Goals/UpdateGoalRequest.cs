namespace Oypa.Crm.Contracts.Goals;

public sealed record UpdateGoalRequest(
    Guid AssigneeEmployeeId,
    string Segment,
    int WeeklyTarget,
    string? Title);
