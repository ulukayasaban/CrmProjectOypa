namespace Oypa.Crm.Contracts.Goals;

public sealed record CreateGoalRequest(
    Guid AssigneeEmployeeId,
    string Segment,
    int WeeklyTarget,
    string? Title);
