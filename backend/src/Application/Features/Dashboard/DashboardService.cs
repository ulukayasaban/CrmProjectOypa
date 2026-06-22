using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Goals;
using Oypa.Crm.Contracts.Dashboard;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Dashboard;

public sealed class DashboardService(
    IRepository<Company> companies,
    IRepository<Meeting> meetings,
    IGoalService goalService,
    IDateTimeProvider clock) : IDashboardService
{
    private static readonly string[] DayLabels = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"];

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var activeLeads = await companies.CountAsync(c => c.Type == CompanyType.Lead, cancellationToken);
        var totalCustomers = await companies.CountAsync(c => c.Type == CompanyType.Customer, cancellationToken);
        var plannedMeetings = await meetings.CountAsync(m => m.Status == MeetingStatus.Planned, cancellationToken);
        var doneMeetings = await meetings.CountAsync(m => m.Status == MeetingStatus.Done, cancellationToken);

        var goals = await goalService.GetScopedProgressAsync(cancellationToken);
        var density = await BuildWeeklyDensityAsync(cancellationToken);

        return new DashboardDto(
            activeLeads, totalCustomers, plannedMeetings, doneMeetings, goals, density);
    }

    private async Task<IReadOnlyList<WeeklyDensityPoint>> BuildWeeklyDensityAsync(CancellationToken cancellationToken)
    {
        var today = clock.Today;
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysSinceMonday);
        var sunday = monday.AddDays(6);

        var weekMeetings = await meetings.ListAsync(
            m => m.Date >= monday && m.Date <= sunday, cancellationToken);

        var counts = new int[7];
        foreach (var m in weekMeetings)
        {
            var idx = ((int)m.Date.DayOfWeek + 6) % 7;
            counts[idx]++;
        }

        return [.. DayLabels.Select((label, i) => new WeeklyDensityPoint(label, counts[i]))];
    }
}
