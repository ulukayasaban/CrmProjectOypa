using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Contracts.Goals;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Goals;

public sealed class GoalService(
    IOrgScopeService orgScope,
    IRepository<Goal> goals,
    IRepository<GoalWeek> goalWeeks,
    IMeetingRepository meetingRepository,
    IRepository<SalesRep> salesReps,
    IRepository<Employee> employees,
    INotificationService notificationService,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : IGoalService
{
    public async Task<IReadOnlyList<GoalDto>> GetScopedAsync(CancellationToken cancellationToken = default)
    {
        var scope = await orgScope.ResolveAsync(cancellationToken);

        var allGoals = await goals.ListAsync(g => g.IsActive, cancellationToken);
        var scopedGoals = FilterByScope(allGoals, scope);

        var today = clock.Today;
        var weekStart = GetMonday(today);

        var result = new List<GoalDto>();
        foreach (var goal in scopedGoals)
        {
            await EnsureSnapshotsAsync(goal, today, cancellationToken);

            var week = await GetOrCreateCurrentWeekAsync(goal, weekStart, cancellationToken);
            var achieved = await ComputeAchievedAsync(goal, weekStart, cancellationToken);
            week.SetAchieved(achieved);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var percent = ComputePercent(achieved, goal.WeeklyTarget);
            var assigneeName = await GetEmployeeNameAsync(goal.AssigneeEmployeeId, cancellationToken);

            result.Add(MapToGoalDto(goal, assigneeName, goal.WeeklyTarget, achieved, percent));
        }

        return result;
    }

    public async Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken = default)
    {
        var scope = await orgScope.ResolveAsync(cancellationToken);

        if (!scope.AllEmployees && !scope.Ids.Contains(request.AssigneeEmployeeId))
            throw new ForbiddenAppException("Hedef atanan personel yönetim kapsamınızın dışında.");

        if (!Enum.TryParse<GoalSegment>(request.Segment, ignoreCase: true, out var segment))
            throw new ConflictException($"Geçersiz segment değeri: {request.Segment}");

        var employee = await employees.GetByIdAsync(request.AssigneeEmployeeId, cancellationToken)
            ?? throw NotFoundException.For("Personel", request.AssigneeEmployeeId);

        var goal = new Goal(request.AssigneeEmployeeId, segment, request.WeeklyTarget, request.Title);
        await goals.AddAsync(goal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Atanan çalışanın bağlı kullanıcısına bildirim gönder (varsa)
        if (employee.ApplicationUserId.HasValue)
        {
            await notificationService.CreateForUsersAsync(
                [employee.ApplicationUserId.Value],
                "Size yeni bir hedef atandı.",
                NotificationType.GoalAssigned,
                title: "Yeni Hedef",
                link: null,
                senderUserId: null,
                senderName: null,
                cancellationToken);
        }

        var assigneeName = employee.FullName ?? employee.Title;
        return MapToGoalDto(goal, assigneeName, 0, 0, 0);
    }

    public async Task<GoalDto> UpdateAsync(Guid id, UpdateGoalRequest request, CancellationToken cancellationToken = default)
    {
        var scope = await orgScope.ResolveAsync(cancellationToken);

        var goal = await goals.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Hedef", id);

        if (!scope.AllEmployees && !scope.Ids.Contains(goal.AssigneeEmployeeId))
            throw new ForbiddenAppException("Bu hedefe erişim yetkiniz yok.");

        if (!Enum.TryParse<GoalSegment>(request.Segment, ignoreCase: true, out var segment))
            throw new ConflictException($"Geçersiz segment değeri: {request.Segment}");

        if (!scope.AllEmployees && !scope.Ids.Contains(request.AssigneeEmployeeId))
            throw new ForbiddenAppException("Hedef atanan personel yönetim kapsamınızın dışında.");

        var employee = await employees.GetByIdAsync(request.AssigneeEmployeeId, cancellationToken)
            ?? throw NotFoundException.For("Personel", request.AssigneeEmployeeId);

        goal.Reassign(request.AssigneeEmployeeId);
        goal.UpdateDetails(segment, request.WeeklyTarget, request.Title);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var assigneeName = employee.FullName ?? employee.Title;
        return MapToGoalDto(goal, assigneeName, 0, 0, 0);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scope = await orgScope.ResolveAsync(cancellationToken);

        var goal = await goals.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Hedef", id);

        if (!scope.AllEmployees && !scope.Ids.Contains(goal.AssigneeEmployeeId))
            throw new ForbiddenAppException("Bu hedefe erişim yetkiniz yok.");

        goal.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GoalWeekDto>> GetWeeksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scope = await orgScope.ResolveAsync(cancellationToken);

        var goal = await goals.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Hedef", id);

        if (!scope.AllEmployees && !scope.Ids.Contains(goal.AssigneeEmployeeId))
            throw new ForbiddenAppException("Bu hedefe erişim yetkiniz yok.");

        var today = clock.Today;
        var weekStart = GetMonday(today);

        await EnsureSnapshotsAsync(goal, today, cancellationToken);

        var currentWeek = await GetOrCreateCurrentWeekAsync(goal, weekStart, cancellationToken);
        var achieved = await ComputeAchievedAsync(goal, weekStart, cancellationToken);
        currentWeek.SetAchieved(achieved);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var allWeeks = await goalWeeks.ListAsync(w => w.GoalId == id, cancellationToken);

        return allWeeks
            .OrderBy(w => w.WeekStart)
            .Select(w => new GoalWeekDto(
                w.WeekStart,
                w.TargetValue,
                w.AchievedCount,
                ComputePercent(w.AchievedCount, w.TargetValue)))
            .ToList();
    }

    public async Task SnapshotAllAsync(CancellationToken cancellationToken = default)
    {
        // Tüm aktif hedefleri çek (kapsam filtresi olmadan; bu bir sistem işlemi).
        var allGoals = await goals.ListAsync(g => g.IsActive, cancellationToken);
        var today = clock.Today;

        foreach (var goal in allGoals)
        {
            // Mevcut EnsureSnapshotsAsync mantığını yeniden kullan; tekrar kod yazılmaz.
            await EnsureSnapshotsAsync(goal, today, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<GoalProgressDto>> GetScopedProgressAsync(CancellationToken cancellationToken = default)
    {
        var scope = await orgScope.ResolveAsync(cancellationToken);

        var allGoals = await goals.ListAsync(g => g.IsActive, cancellationToken);
        var scopedGoals = FilterByScope(allGoals, scope);

        var today = clock.Today;
        var weekStart = GetMonday(today);
        var weekEnd = weekStart.AddDays(6);

        var result = new List<GoalProgressDto>();
        foreach (var goal in scopedGoals)
        {
            var subtree = await orgScope.GetSubtreeIdsAsync(goal.AssigneeEmployeeId, cancellationToken);
            var repIds = (await salesReps.ListAsync(
                r => r.EmployeeId.HasValue && subtree.Contains(r.EmployeeId.Value),
                cancellationToken))
                .Select(r => r.Id)
                .ToList();

            var achieved = await ComputeAchievedAsync(goal, weekStart, cancellationToken);
            var percent = ComputePercent(achieved, goal.WeeklyTarget);
            var assigneeName = await GetEmployeeNameAsync(goal.AssigneeEmployeeId, cancellationToken);

            int newCustomerAchieved = 0;
            int existingCustomerAchieved = 0;
            if (repIds.Count > 0)
            {
                (newCustomerAchieved, existingCustomerAchieved) =
                    await meetingRepository.CountDoneByRepsCustomerBreakdownAsync(
                        repIds, weekStart, weekEnd, cancellationToken);
            }

            result.Add(new GoalProgressDto(
                goal.Id,
                assigneeName,
                goal.Segment.ToString(),
                goal.WeeklyTarget,
                achieved,
                percent,
                newCustomerAchieved,
                existingCustomerAchieved));
        }

        return result;
    }

    // -----------------------------------------------------------------------
    // Yardımcı metotlar
    // -----------------------------------------------------------------------

    private static IReadOnlyList<Goal> FilterByScope(IReadOnlyList<Goal> allGoals, OrgScope scope)
    {
        if (scope.AllEmployees)
            return allGoals;

        return allGoals.Where(g => scope.Ids.Contains(g.AssigneeEmployeeId)).ToList();
    }

    private async Task EnsureSnapshotsAsync(Goal goal, DateOnly today, CancellationToken cancellationToken)
    {
        var currentWeekStart = GetMonday(today);
        var goalCreatedWeekStart = GetMonday(DateOnly.FromDateTime(goal.CreatedAtUtc));

        var existingWeeks = await goalWeeks.ListAsync(w => w.GoalId == goal.Id, cancellationToken);
        var existingStarts = existingWeeks.Select(w => w.WeekStart).ToHashSet();

        var weekCursor = goalCreatedWeekStart;
        while (weekCursor <= currentWeekStart)
        {
            if (!existingStarts.Contains(weekCursor))
            {
                int achieved = 0;
                if (weekCursor < currentWeekStart)
                    achieved = await ComputeAchievedAsync(goal, weekCursor, cancellationToken);

                var newWeek = new GoalWeek(goal.Id, weekCursor, goal.WeeklyTarget);
                newWeek.SetAchieved(achieved);
                await goalWeeks.AddAsync(newWeek, cancellationToken);
            }

            weekCursor = weekCursor.AddDays(7);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<GoalWeek> GetOrCreateCurrentWeekAsync(
        Goal goal,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var existing = (await goalWeeks.ListAsync(
            w => w.GoalId == goal.Id && w.WeekStart == weekStart,
            cancellationToken)).FirstOrDefault();

        if (existing is not null)
            return await goalWeeks.GetByIdAsync(existing.Id, cancellationToken) ?? existing;

        var newWeek = new GoalWeek(goal.Id, weekStart, goal.WeeklyTarget);
        await goalWeeks.AddAsync(newWeek, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return newWeek;
    }

    private async Task<int> ComputeAchievedAsync(
        Goal goal,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var subtree = await orgScope.GetSubtreeIdsAsync(goal.AssigneeEmployeeId, cancellationToken);

        var repIds = (await salesReps.ListAsync(
            r => r.EmployeeId.HasValue && subtree.Contains(r.EmployeeId.Value),
            cancellationToken))
            .Select(r => r.Id)
            .ToList();

        if (repIds.Count == 0)
            return 0;

        var weekEnd = weekStart.AddDays(6);
        return await meetingRepository.CountDoneByRepsAndSegmentAsync(
            repIds,
            weekStart,
            weekEnd,
            goal.Segment,
            cancellationToken);
    }

    private async Task<string?> GetEmployeeNameAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employee = await employees.GetByIdAsync(employeeId, cancellationToken);
        return employee?.FullName ?? employee?.Title;
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }

    private static int ComputePercent(int achieved, int target) =>
        target > 0 ? Math.Min(100, (int)Math.Round((double)achieved / target * 100)) : 0;

    private static GoalDto MapToGoalDto(Goal goal, string? assigneeName, int target, int achieved, int percent) =>
        new(
            goal.Id,
            goal.AssigneeEmployeeId,
            assigneeName,
            goal.Segment.ToString(),
            goal.WeeklyTarget,
            goal.Title,
            goal.IsActive,
            target,
            achieved,
            percent);
}
