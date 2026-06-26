namespace Oypa.Crm.Contracts.Goals;

/// <summary>Dashboard'da gösterilen hedef ilerleme özeti.</summary>
public sealed record GoalProgressDto(
    Guid GoalId,
    string? AssigneeName,
    string Segment,
    int WeeklyTarget,
    int Achieved,
    int Percent,
    int NewCustomerAchieved,
    int ExistingCustomerAchieved);
