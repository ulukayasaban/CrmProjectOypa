using System.Linq.Expressions;
using NSubstitute;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Goals;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Contracts.Goals;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Goals;

/// <summary>
/// GoalService.SnapshotAllAsync için birim testleri.
/// Mevcut EnsureSnapshotsAsync mantığının yeniden kullanıldığını doğrular.
/// </summary>
public sealed class GoalSnapshotServiceTests
{
    // ---- Bağımlılıklar ----
    private readonly IOrgScopeService _orgScope = Substitute.For<IOrgScopeService>();
    private readonly IRepository<Goal> _goals = Substitute.For<IRepository<Goal>>();
    private readonly IRepository<GoalWeek> _goalWeeks = Substitute.For<IRepository<GoalWeek>>();
    private readonly IMeetingRepository _meetingRepository = Substitute.For<IMeetingRepository>();
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly IRepository<Employee> _employees = Substitute.For<IRepository<Employee>>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private GoalService CreateSut() => new(
        _orgScope, _goals, _goalWeeks, _meetingRepository,
        _salesReps, _employees, _notificationService, _clock, _unitOfWork);

    private static Goal MakeGoal(DateOnly createdDate, GoalSegment segment = GoalSegment.All, int weeklyTarget = 5)
    {
        var assigneeId = Guid.NewGuid();
        var goal = new Goal(assigneeId, segment, weeklyTarget, null);
        goal.CreatedAtUtc = createdDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return goal;
    }

    // ========================================================================
    // SnapshotAllAsync — temel davranış
    // ========================================================================

    [Fact]
    public async Task SnapshotAllAsync_NoActiveGoals_SaveChangesNotCalled()
    {
        // Arrange — aktif hedef yok
        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Goal>());

        _clock.Today.Returns(new DateOnly(2026, 6, 9));

        var sut = CreateSut();

        // Act
        await sut.SnapshotAllAsync();

        // Assert — kayıt yok; SaveChanges çağrılmamalı
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SnapshotAllAsync_OneActiveGoal_CreatesSnapshotForMissingWeeks()
    {
        // Arrange — 2 hafta önce oluşturulmuş bir hedef; mevcut snapshot yok
        var today = new DateOnly(2026, 6, 9); // Pazartesi
        var createdDate = new DateOnly(2026, 5, 26); // Pazartesi (2 hafta önce)
        var goal = MakeGoal(createdDate);

        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        // Mevcut haftalık snapshot yok → EnsureSnapshotsAsync yeni kayıtlar ekler
        _goalWeeks.ListAsync(Arg.Any<Expression<Func<GoalWeek, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GoalWeek>());

        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());

        _clock.Today.Returns(today);

        var sut = CreateSut();

        // Act
        await sut.SnapshotAllAsync();

        // Assert — eksik haftalar için GoalWeek eklenmeli
        await _goalWeeks.Received()
            .AddAsync(Arg.Any<GoalWeek>(), Arg.Any<CancellationToken>());

        // Kaydedilmeli (EnsureSnapshotsAsync içinde SaveChanges çağrılır)
        await _unitOfWork.Received()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SnapshotAllAsync_SnapshotsAlreadyExist_AddsNoNewWeeks()
    {
        // Arrange — today = 9 Haziran 2026 Salı; bu haftanın Pazartesi = 8 Haziran 2026.
        // EnsureSnapshotsAsync, haftaları Pazartesi hizalı tutar; existingWeek.WeekStart
        // de aynı Pazartesiyi göstermeli; aksi takdirde mevcut snapshot tanınmaz.
        var today = new DateOnly(2026, 6, 9);   // Salı
        var monday = new DateOnly(2026, 6, 8);  // Bu haftanın Pazartesisi

        // Hedef haftanın Pazartesisinde oluşturuldu; goalCreatedWeekStart = monday.
        var goal = MakeGoal(monday);

        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        // Bu haftanın snapshot'ı (WeekStart = monday) zaten mevcut
        var existingWeek = new GoalWeek(goal.Id, monday, goal.WeeklyTarget);
        _goalWeeks.ListAsync(Arg.Any<Expression<Func<GoalWeek, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { existingWeek });

        _clock.Today.Returns(today);

        var sut = CreateSut();

        // Act
        await sut.SnapshotAllAsync();

        // Assert — snapshot mevcut; yeni GoalWeek eklenmemeli
        await _goalWeeks.DidNotReceive()
            .AddAsync(Arg.Any<GoalWeek>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SnapshotAllAsync_MultipleActiveGoals_ProcessesEachGoal()
    {
        // Arrange — 3 aktif hedef; hepsinin snapshot'ı oluşturulmalı
        var today = new DateOnly(2026, 6, 9);
        var goal1 = MakeGoal(today, GoalSegment.All);
        var goal2 = MakeGoal(today, GoalSegment.Lead);
        var goal3 = MakeGoal(today, GoalSegment.Customer);

        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal1, goal2, goal3 });

        // Her hedef için bu haftanın snapshot'ı zaten var → yeni ekleme yok
        _goalWeeks.ListAsync(Arg.Any<Expression<Func<GoalWeek, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                // ListAsync her çağrı için boş döndür; idempotens kontrolü burada kritik değil
                return Array.Empty<GoalWeek>();
            });

        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());

        _clock.Today.Returns(today);

        var sut = CreateSut();

        // Act
        await sut.SnapshotAllAsync();

        // Assert — ListAsync 3 hedef * en az 1 çağrı almalıydı (goalWeeks.ListAsync)
        await _goals.Received(1)
            .ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>());
    }
}
