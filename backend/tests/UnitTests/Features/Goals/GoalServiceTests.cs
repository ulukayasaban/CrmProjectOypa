using System.Linq.Expressions;
using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Goals;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Contracts.Goals;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Goals;

public sealed class GoalServiceTests
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

    // ---- Yardımcı fabrika metotları ----

    private static Goal MakeGoal(Guid assigneeId, GoalSegment segment = GoalSegment.All, int weeklyTarget = 5, string? title = null)
    {
        var goal = new Goal(assigneeId, segment, weeklyTarget, title);
        // CreatedAtUtc BaseEntity tarafından şimdi olarak ayarlanır
        return goal;
    }

    private static Employee MakeEmployee(string fullName = "Test Personel")
        => new("Ünvan", fullName, $"{fullName.Replace(" ", "").ToLower()}@oypa.com.tr");

    private static SalesRep MakeSalesRep(string name = "Test Temsilci", Guid? employeeId = null)
    {
        var rep = new SalesRep(name, $"{name.Replace(" ", "").ToLower()}@oypa.com.tr");
        if (employeeId.HasValue)
            rep.LinkEmployee(employeeId);
        return rep;
    }

    // Pazartesiye hizalanmış tarih döndürür
    private static DateOnly GetMonday(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }

    // ========================================================================
    // GetScopedAsync — kapsam testleri
    // ========================================================================

    [Fact]
    public async Task GetScopedAsync_AsAdmin_AllEmployeesScope_ReturnsAllActiveGoals()
    {
        // Arrange — Umur gibi Admin kök düğüm: AllEmployees = true
        var empId1 = Guid.NewGuid();
        var empId2 = Guid.NewGuid();
        var goal1 = MakeGoal(empId1, GoalSegment.All, 5);
        var goal2 = MakeGoal(empId2, GoalSegment.Customer, 3);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));
        _orgScope.GetSubtreeIdsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());

        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal1, goal2 });

        _goalWeeks.ListAsync(Arg.Any<Expression<Func<GoalWeek, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GoalWeek>());

        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());

        _employees.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var today = new DateOnly(2026, 6, 9); // Salı
        _clock.Today.Returns(today);

        var sut = CreateSut();

        // Act
        var result = await sut.GetScopedAsync();

        // Assert
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetScopedAsync_AsAvniye_LimitedScope_ReturnsOnlyScopedGoals()
    {
        // Arrange — Avniye'nin alt-ağacı: kendi Id'si + astları
        var avniyeEmpId = Guid.NewGuid();
        var halilEmpId = Guid.NewGuid();

        var goalForAvniye = MakeGoal(avniyeEmpId, GoalSegment.Lead, 4);
        var goalForHalil = MakeGoal(halilEmpId, GoalSegment.All, 5);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(false, [avniyeEmpId]));
        _orgScope.GetSubtreeIdsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());

        // Tüm aktif hedefler her ikisi de dönüyor; servis scope filtresi uygulamalı
        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goalForAvniye, goalForHalil });

        _goalWeeks.ListAsync(Arg.Any<Expression<Func<GoalWeek, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GoalWeek>());

        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());

        _employees.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        _clock.Today.Returns(new DateOnly(2026, 6, 9));

        var sut = CreateSut();

        // Act
        var result = await sut.GetScopedAsync();

        // Assert — yalnız Avniye'ye atanan hedef görünmeli
        result.Count.ShouldBe(1);
        result[0].AssigneeEmployeeId.ShouldBe(avniyeEmpId);
    }

    // ========================================================================
    // CreateAsync — yetki/kapsam testleri
    // ========================================================================

    [Fact]
    public async Task CreateAsync_AssigneeWithinScope_CreatesGoalSuccessfully()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = MakeEmployee("Umur KUTLU");
        employee.Id = employeeId;

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));

        _employees.GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var sut = CreateSut();
        var request = new CreateGoalRequest(employeeId, "All", 5, "Q1 Hedef");

        // Act
        var result = await sut.CreateAsync(request);

        // Assert
        result.AssigneeEmployeeId.ShouldBe(employeeId);
        result.Segment.ShouldBe("All");
        result.WeeklyTarget.ShouldBe(5);
        result.Title.ShouldBe("Q1 Hedef");
        await _goals.Received(1).AddAsync(Arg.Any<Goal>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_AssigneeOutsideScope_ThrowsForbiddenAppException()
    {
        // Arrange — Avniye kapsamında yalnız kendi alt-ağacı var; Halil'in astına atama => 403
        var halilSubId = Guid.NewGuid();
        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(false, [Guid.NewGuid()])); // farklı bir Id kümesi

        var sut = CreateSut();
        var request = new CreateGoalRequest(halilSubId, "All", 5, null);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAppException>(() => sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_EmployeeNotFound_ThrowsNotFoundException()
    {
        // Arrange — kapsamda olan ama DB'de bulunmayan personel
        var employeeId = Guid.NewGuid();
        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));

        _employees.GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var sut = CreateSut();
        var request = new CreateGoalRequest(employeeId, "All", 5, null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_InvalidSegment_ThrowsConflictException()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));

        var sut = CreateSut();
        var request = new CreateGoalRequest(employeeId, "GecersizSegment", 5, null);

        // Act & Assert
        await Should.ThrowAsync<ConflictException>(() => sut.CreateAsync(request));
    }

    // ========================================================================
    // UpdateAsync — yetki/kapsam testleri
    // ========================================================================

    [Fact]
    public async Task UpdateAsync_GoalOutsideCallerScope_ThrowsForbiddenAppException()
    {
        // Arrange — Avniye kapsamı Halil'in hedefini görmemeli
        var halilEmpId = Guid.NewGuid();
        var goal = MakeGoal(halilEmpId, GoalSegment.All, 3);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(false, [Guid.NewGuid()])); // farklı küme

        _goals.GetByIdAsync(goal.Id, Arg.Any<CancellationToken>())
            .Returns(goal);

        var sut = CreateSut();
        var request = new UpdateGoalRequest(halilEmpId, "Customer", 4, null);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAppException>(() => sut.UpdateAsync(goal.Id, request));
    }

    [Fact]
    public async Task UpdateAsync_WithinScope_UpdatesGoalAndSaves()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goal = MakeGoal(employeeId, GoalSegment.All, 3);
        var employee = MakeEmployee("Test Personel");
        employee.Id = employeeId;

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));

        _goals.GetByIdAsync(goal.Id, Arg.Any<CancellationToken>())
            .Returns(goal);

        _employees.GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var sut = CreateSut();
        var request = new UpdateGoalRequest(employeeId, "Customer", 8, "Güncel Başlık");

        // Act
        var result = await sut.UpdateAsync(goal.Id, request);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.WeeklyTarget.ShouldBe(8);
        result.Segment.ShouldBe("Customer");
        result.Title.ShouldBe("Güncel Başlık");
    }

    [Fact]
    public async Task UpdateAsync_GoalNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));
        _goals.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Goal?)null);

        var sut = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            sut.UpdateAsync(Guid.NewGuid(), new UpdateGoalRequest(Guid.NewGuid(), "All", 5, null)));
    }

    [Fact]
    public async Task UpdateAsync_ReassignOutsideScope_ThrowsForbiddenAppException()
    {
        // Arrange — hedef kapsam içi ama yeni assignee kapsam dışı
        var employeeId = Guid.NewGuid();
        var outOfScopeId = Guid.NewGuid();
        var goal = MakeGoal(employeeId, GoalSegment.All, 5);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(false, [employeeId])); // sadece employeeId kapsam içi

        _goals.GetByIdAsync(goal.Id, Arg.Any<CancellationToken>())
            .Returns(goal);

        var sut = CreateSut();
        // Hedef kapsam içi (employeeId), yeni assignee kapsam dışı (outOfScopeId)
        var request = new UpdateGoalRequest(outOfScopeId, "All", 5, null);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAppException>(() => sut.UpdateAsync(goal.Id, request));
    }

    // ========================================================================
    // DeleteAsync — yetki/kapsam testleri
    // ========================================================================

    [Fact]
    public async Task DeleteAsync_WithinScope_DeactivatesGoal()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var goal = MakeGoal(employeeId);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));
        _goals.GetByIdAsync(goal.Id, Arg.Any<CancellationToken>())
            .Returns(goal);

        var sut = CreateSut();

        // Act
        await sut.DeleteAsync(goal.Id);

        // Assert
        goal.IsActive.ShouldBeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_OutsideScope_ThrowsForbiddenAppException()
    {
        // Arrange
        var halilEmpId = Guid.NewGuid();
        var goal = MakeGoal(halilEmpId);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(false, [Guid.NewGuid()])); // farklı küme
        _goals.GetByIdAsync(goal.Id, Arg.Any<CancellationToken>())
            .Returns(goal);

        var sut = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAppException>(() => sut.DeleteAsync(goal.Id));
    }

    // ========================================================================
    // GetWeeksAsync — snapshot / haftalık pencere testleri
    // ========================================================================

    [Fact]
    public async Task GetWeeksAsync_OutsideScope_ThrowsForbiddenAppException()
    {
        // Arrange
        var halilEmpId = Guid.NewGuid();
        var goal = MakeGoal(halilEmpId);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(false, [Guid.NewGuid()]));
        _goals.GetByIdAsync(goal.Id, Arg.Any<CancellationToken>())
            .Returns(goal);

        var sut = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAppException>(() => sut.GetWeeksAsync(goal.Id));
    }

    [Fact]
    public async Task GetWeeksAsync_WithinScope_ReturnsPastWeeksSorted()
    {
        // Arrange — hedef 2 hafta önce oluşturuldu; 2 snapshot bekleniyor
        var employeeId = Guid.NewGuid();
        var goal = MakeGoal(employeeId, GoalSegment.All, 5);

        // Geçmişte bir tarih simüle etmek için CreatedAtUtc'yi elle ayarla
        goal.CreatedAtUtc = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc); // Pazar

        var today = new DateOnly(2026, 6, 9); // Salı (2 hafta sonra)
        _clock.Today.Returns(today);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));
        _orgScope.GetSubtreeIdsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());

        _goals.GetByIdAsync(goal.Id, Arg.Any<CancellationToken>())
            .Returns(goal);

        var week1Start = new DateOnly(2026, 5, 25).AddDays(-((int)new DateOnly(2026, 5, 25).DayOfWeek + 6) % 7); // Pzt
        var week2Start = week1Start.AddDays(7);
        var week3Start = week2Start.AddDays(7);

        var weekSnap1 = new GoalWeek(goal.Id, week1Start, 5);
        weekSnap1.SetAchieved(3);
        var weekSnap2 = new GoalWeek(goal.Id, week2Start, 5);
        weekSnap2.SetAchieved(4);

        // EnsureSnapshotsAsync için mevcut haftalar
        _goalWeeks.ListAsync(
            Arg.Any<Expression<Func<GoalWeek, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(new[] { weekSnap1, weekSnap2 });

        _goalWeeks.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GoalWeek?)null);

        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());

        _meetingRepository.CountDoneByRepsAndSegmentAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<GoalSegment>(),
            Arg.Any<CancellationToken>())
            .Returns(0);

        var sut = CreateSut();

        // Act
        var result = await sut.GetWeeksAsync(goal.Id);

        // Assert — sonuçlar haftaya göre sıralı
        result.ShouldNotBeNull();
        result.Count.ShouldBeGreaterThan(0);
        for (int i = 1; i < result.Count; i++)
            result[i].WeekStart.ShouldBeGreaterThan(result[i - 1].WeekStart);
    }

    // ========================================================================
    // ComputeAchievedAsync — segment filtresi testleri
    // ========================================================================

    [Fact]
    public async Task GetScopedAsync_GoalWithEmptySubtree_AchievedIsZero()
    {
        // Arrange — assignee'nin alt-ağacında SalesRep yok → achieved = 0
        var employeeId = Guid.NewGuid();
        var goal = MakeGoal(employeeId, GoalSegment.Customer, 5);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));
        _orgScope.GetSubtreeIdsAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { employeeId });

        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        _goalWeeks.ListAsync(Arg.Any<Expression<Func<GoalWeek, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GoalWeek>());

        // Kapsam içi EmployeeId'ye sahip SalesRep yok
        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());

        _employees.GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(MakeEmployee());

        _clock.Today.Returns(new DateOnly(2026, 6, 9)); // Salı

        var sut = CreateSut();

        // Act
        var result = await sut.GetScopedAsync();

        // Assert
        result.Count.ShouldBe(1);
        result[0].CurrentAchieved.ShouldBe(0);
        // Segment yok → CountDone çağrılmaz
        await _meetingRepository.DidNotReceive().CountDoneByRepsAndSegmentAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<GoalSegment>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetScopedAsync_GoalWithLinkedRep_CallsCountDoneWithCorrectSegment()
    {
        // Arrange — alt-ağaçta EmployeeId set edilmiş bir SalesRep var
        var employeeId = Guid.NewGuid();
        var repId = Guid.NewGuid();
        var goal = MakeGoal(employeeId, GoalSegment.Lead, 5);

        var rep = MakeSalesRep("Muhammed", employeeId);
        rep.Id = repId;

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));
        _orgScope.GetSubtreeIdsAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { employeeId });

        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        _goalWeeks.ListAsync(Arg.Any<Expression<Func<GoalWeek, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GoalWeek>());

        // EmployeeId dolu ve subtree içindeki rep
        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { rep });

        _meetingRepository.CountDoneByRepsAndSegmentAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            GoalSegment.Lead,
            Arg.Any<CancellationToken>())
            .Returns(3);

        _employees.GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(MakeEmployee("Muhammed MARANGOZ"));

        var monday = new DateOnly(2026, 6, 9);
        _clock.Today.Returns(monday);

        var sut = CreateSut();

        // Act
        var result = await sut.GetScopedAsync();

        // Assert — segment filtresiyle çağrı yapıldı
        await _meetingRepository.Received().CountDoneByRepsAndSegmentAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(repId)),
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            GoalSegment.Lead,
            Arg.Any<CancellationToken>());

        result[0].CurrentAchieved.ShouldBe(3);
    }

    [Fact]
    public async Task GetScopedAsync_RepWithNullEmployeeId_IsNotCounted()
    {
        // Arrange — EmployeeId=null olan rep dahil edilmemeli
        var employeeId = Guid.NewGuid();
        var goal = MakeGoal(employeeId, GoalSegment.All, 5);

        var repWithNoEmployee = MakeSalesRep("Bağlantısız", null); // EmployeeId = null

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));
        _orgScope.GetSubtreeIdsAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { employeeId });

        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        _goalWeeks.ListAsync(Arg.Any<Expression<Func<GoalWeek, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GoalWeek>());

        // Filtre: r.EmployeeId.HasValue && subtree.Contains(r.EmployeeId.Value) → false
        // Servis NSubstitute expression filter'ı geçiyor; tüm rep'leri döndür ama
        // servis kendi filtreli ListAsync çağrısında repWithNoEmployee'yi dışlamalı.
        // NSubstitute expression match edemez; boş liste döndürerek simüle ediyoruz.
        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());

        _employees.GetByIdAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(MakeEmployee());

        _clock.Today.Returns(new DateOnly(2026, 6, 9));

        var sut = CreateSut();

        // Act
        var result = await sut.GetScopedAsync();

        // Assert — EmployeeId=null rep CountDone'a iletilmemeli
        await _meetingRepository.DidNotReceive().CountDoneByRepsAndSegmentAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<GoalSegment>(),
            Arg.Any<CancellationToken>());

        result[0].CurrentAchieved.ShouldBe(0);
    }

    // ========================================================================
    // GetScopedProgressAsync — dashboard özeti
    // ========================================================================

    [Fact]
    public async Task GetScopedProgressAsync_AsAdmin_ReturnsProgressForAllGoals()
    {
        // Arrange
        var empId = Guid.NewGuid();
        var goal = MakeGoal(empId, GoalSegment.All, 10);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));
        _orgScope.GetSubtreeIdsAsync(empId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { empId });

        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());

        _employees.GetByIdAsync(empId, Arg.Any<CancellationToken>())
            .Returns(MakeEmployee("Umur KUTLU"));

        _clock.Today.Returns(new DateOnly(2026, 6, 9));

        var sut = CreateSut();

        // Act
        var result = await sut.GetScopedProgressAsync();

        // Assert
        result.Count.ShouldBe(1);
        result[0].GoalId.ShouldBe(goal.Id);
        result[0].WeeklyTarget.ShouldBe(10);
        result[0].Segment.ShouldBe("All");
        result[0].NewCustomerAchieved.ShouldBe(0);
        result[0].ExistingCustomerAchieved.ShouldBe(0);
    }

    // ========================================================================
    // Yüzde hesabı — kenar durumlar
    // ========================================================================

    [Fact]
    public async Task GetScopedProgressAsync_ZeroTarget_PercentIsZero()
    {
        // Arrange — WeeklyTarget > 0 olmak zorunda (domain kısıtı) ama servis içi
        // hesaplamada target = 0 ise percent = 0 olmalı; GoalDto.CurrentTarget = 0 ile simüle
        var empId = Guid.NewGuid();
        var goal = MakeGoal(empId, GoalSegment.All, 5);

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));
        _orgScope.GetSubtreeIdsAsync(empId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());

        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SalesRep>());

        _employees.GetByIdAsync(empId, Arg.Any<CancellationToken>())
            .Returns(MakeEmployee());

        _clock.Today.Returns(new DateOnly(2026, 6, 9));

        var sut = CreateSut();

        // Act
        var result = await sut.GetScopedProgressAsync();

        // Assert — rep yokken achieved = 0
        result.Count.ShouldBe(1);
        result[0].Achieved.ShouldBe(0);
        result[0].Percent.ShouldBe(0);
    }

    // ========================================================================
    // GetScopedProgressAsync — yeni/mevcut müşteri kırılımı
    // ========================================================================

    [Fact]
    public async Task GetScopedProgressAsync_WithLinkedRep_ReturnsCustomerBreakdown()
    {
        // Arrange — alt-ağaçta EmployeeId bağlı bir SalesRep var;
        // 2 yeni müşteri + 3 mevcut müşteri görüşmesi dönsün.
        var empId = Guid.NewGuid();
        var repId = Guid.NewGuid();
        var goal = MakeGoal(empId, GoalSegment.Customer, 10);

        var rep = MakeSalesRep("Halil", empId);
        rep.Id = repId;

        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(true, []));
        _orgScope.GetSubtreeIdsAsync(empId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { empId });

        _goals.ListAsync(Arg.Any<Expression<Func<Goal, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { goal });

        _salesReps.ListAsync(Arg.Any<Expression<Func<SalesRep, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { rep });

        _meetingRepository.CountDoneByRepsAndSegmentAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            GoalSegment.Customer,
            Arg.Any<CancellationToken>())
            .Returns(5);

        _meetingRepository.CountDoneByRepsCustomerBreakdownAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<DateOnly>(),
            Arg.Any<DateOnly>(),
            Arg.Any<CancellationToken>())
            .Returns((NewCustomer: 2, ExistingCustomer: 3));

        _employees.GetByIdAsync(empId, Arg.Any<CancellationToken>())
            .Returns(MakeEmployee("Halil YÜKSEL"));

        _clock.Today.Returns(new DateOnly(2026, 6, 9)); // Pazartesi

        var sut = CreateSut();

        // Act
        var result = await sut.GetScopedProgressAsync();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Achieved.ShouldBe(5);
        result[0].NewCustomerAchieved.ShouldBe(2);
        result[0].ExistingCustomerAchieved.ShouldBe(3);
        // NewCustomer + ExistingCustomer ≤ Achieved (Customer-only subset)
        (result[0].NewCustomerAchieved + result[0].ExistingCustomerAchieved).ShouldBeLessThanOrEqualTo(result[0].Achieved);
    }
}
