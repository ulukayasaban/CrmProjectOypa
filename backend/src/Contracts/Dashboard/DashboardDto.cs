using Oypa.Crm.Contracts.Goals;

namespace Oypa.Crm.Contracts.Dashboard;

public sealed record DashboardDto(
    int ActiveLeads,
    int TotalCustomers,
    int PlannedMeetings,
    int DoneMeetings,
    IReadOnlyList<GoalProgressDto> Goals,
    IReadOnlyList<WeeklyDensityPoint> WeeklyDensity);
