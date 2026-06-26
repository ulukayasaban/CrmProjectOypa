using System.Linq.Expressions;
using NSubstitute;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Dashboard;
using Oypa.Crm.Application.Features.Goals;
using Oypa.Crm.Contracts.Goals;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Dashboard;

public sealed class DashboardServiceTests
{
    private readonly IRepository<Company> _companies = Substitute.For<IRepository<Company>>();
    private readonly IRepository<Meeting> _meetings = Substitute.For<IRepository<Meeting>>();
    private readonly IGoalService _goalService = Substitute.For<IGoalService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private DashboardService CreateSut() => new(_companies, _meetings, _goalService, _clock);

    private static Meeting MeetingOn(DateOnly date) =>
        Meeting.Schedule(Guid.NewGuid(), Guid.NewGuid(), null, date, new TimeOnly(9, 0), "A", MeetingMethod.Visit);

    [Fact]
    public async Task GetAsync_ReturnsGoalsFromGoalService_AndCorrectDensity()
    {
        // 2026-06-08 is a Monday; fix "today" to it so the week is Mon..Sun.
        var monday = new DateOnly(2026, 6, 8);
        _clock.Today.Returns(monday);

        _companies.CountAsync(Arg.Any<Expression<Func<Company, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _meetings.CountAsync(
            Arg.Any<Expression<Func<Meeting, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var goalProgress = new List<GoalProgressDto>
        {
            new(Guid.NewGuid(), "Umur KUTLU", "All", 5, 2, 40, 1, 1)
        };
        _goalService.GetScopedProgressAsync(Arg.Any<CancellationToken>())
            .Returns(goalProgress);

        // Density: 1 meeting Monday (idx 0), 2 meetings Wednesday (idx 2).
        _meetings.ListAsync(Arg.Any<Expression<Func<Meeting, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                MeetingOn(monday),
                MeetingOn(monday.AddDays(2)),
                MeetingOn(monday.AddDays(2)),
            });

        var sut = CreateSut();

        var result = await sut.GetAsync();

        result.Goals.Count.ShouldBe(1);
        result.Goals[0].Percent.ShouldBe(40);
        result.WeeklyDensity.Count.ShouldBe(7);
        result.WeeklyDensity[0].Count.ShouldBe(1); // Pzt
        result.WeeklyDensity[1].Count.ShouldBe(0); // Sal
        result.WeeklyDensity[2].Count.ShouldBe(2); // Çar
        result.WeeklyDensity.Sum(d => d.Count).ShouldBe(3);
    }
}
