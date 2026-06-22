namespace Oypa.Crm.Contracts.Goals;

public sealed record GoalDto(
    Guid Id,
    Guid AssigneeEmployeeId,
    string? AssigneeName,
    string Segment,
    int WeeklyTarget,
    string? Title,
    bool IsActive,
    int CurrentTarget,
    int CurrentAchieved,
    int CurrentPercent);
