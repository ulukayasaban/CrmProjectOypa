using Microsoft.Extensions.Logging;
using NSubstitute;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Events;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Domain.Events;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Notifications;

/// <summary>
/// MeetingScheduledNotificationHandler birim testleri:
/// görüşme planlandığında SalesRep'in yönetici zincirine bildirim üretildiğini doğrular.
/// </summary>
public sealed class MeetingScheduledNotificationHandlerTests
{
    private readonly IOrgScopeService _orgScope = Substitute.For<IOrgScopeService>();
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly IRepository<Employee> _employees = Substitute.For<IRepository<Employee>>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ILogger<MeetingScheduledNotificationHandler> _logger =
        Substitute.For<ILogger<MeetingScheduledNotificationHandler>>();

    private MeetingScheduledNotificationHandler CreateSut() =>
        new(_orgScope, _salesReps, _employees, _notificationService, _logger);

    private static SalesRep MakeSalesRepWithEmployee(Guid employeeId)
    {
        var rep = new SalesRep("Temsilci", "rep@oypa.com");
        rep.LinkEmployee(employeeId);
        return rep;
    }

    // -----------------------------------------------------------------------
    // Mutlu yol: yönetici zincirine bildirim üretilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_RepWithManagerChain_CreatesNotificationsForAncestors()
    {
        var meetingId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var repId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var managerUserId = Guid.NewGuid();
        var rootManagerUserId = Guid.NewGuid();
        var repUserId = Guid.NewGuid();

        var rep = MakeSalesRepWithEmployee(employeeId);
        rep.Id = repId;

        var repEmployee = new Employee("Uzman", "Muhammed MARANGOZ", "m@oypa.com");
        repEmployee.LinkAccount(repUserId);

        _salesReps.GetByIdAsync(repId, Arg.Any<CancellationToken>()).Returns(rep);
        _employees.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(repEmployee);
        _orgScope.GetAncestorUserIdsAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { managerUserId, rootManagerUserId });

        var domainEvent = new MeetingScheduledEvent(meetingId, companyId, repId, null);
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        // rep + 2 yönetici = 3 alıcı
        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(managerUserId)
                                            && ids.Contains(rootManagerUserId)
                                            && ids.Contains(repUserId)),
            Arg.Any<string>(),
            NotificationType.MeetingScheduled,
            Arg.Any<string?>(),
            Arg.Is<string?>(l => l != null && l.Contains(companyId.ToString())),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RepWithNoManagerChain_StillCreatesNotificationForRepItself()
    {
        // Yönetici zinciri boş ama reppin kendi hesabı var
        var repId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var repUserId = Guid.NewGuid();

        var rep = MakeSalesRepWithEmployee(employeeId);
        rep.Id = repId;

        var repEmployee = new Employee("Uzman", "Muhammed", "m@oypa.com");
        repEmployee.LinkAccount(repUserId);

        _salesReps.GetByIdAsync(repId, Arg.Any<CancellationToken>()).Returns(rep);
        _employees.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(repEmployee);
        _orgScope.GetAncestorUserIdsAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>()); // Yönetici zinciri yok

        var domainEvent = new MeetingScheduledEvent(Guid.NewGuid(), Guid.NewGuid(), repId, null);
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(repUserId)),
            Arg.Any<string>(),
            NotificationType.MeetingScheduled,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Edge case'ler: erken çıkış senaryoları
    // -----------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_RepNotFound_DoesNotCreateNotifications()
    {
        _salesReps.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SalesRep?)null);

        var domainEvent = new MeetingScheduledEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        await _notificationService.DidNotReceive().CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RepWithNoEmployee_DoesNotCreateNotifications()
    {
        // Rep org yapısına bağlı değil (EmployeeId = null)
        var repId = Guid.NewGuid();
        var rep = new SalesRep("Temsilci", "rep@oypa.com"); // EmployeeId set edilmedi
        rep.Id = repId;

        _salesReps.GetByIdAsync(repId, Arg.Any<CancellationToken>()).Returns(rep);

        var domainEvent = new MeetingScheduledEvent(Guid.NewGuid(), Guid.NewGuid(), repId, null);
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        await _notificationService.DidNotReceive().CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RepEmployeeHasNoLinkedUser_StillNotifiesAncestors()
    {
        // Reppin Employee kaydı var ama ApplicationUserId yok; yöneticiler yine bildirim almalı
        var repId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var managerUserId = Guid.NewGuid();

        var rep = MakeSalesRepWithEmployee(employeeId);
        rep.Id = repId;

        var repEmployee = new Employee("Uzman", "Muhammed", "m@oypa.com");
        // ApplicationUserId set edilmemiş → null

        _salesReps.GetByIdAsync(repId, Arg.Any<CancellationToken>()).Returns(rep);
        _employees.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(repEmployee);
        _orgScope.GetAncestorUserIdsAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { managerUserId });

        var domainEvent = new MeetingScheduledEvent(Guid.NewGuid(), Guid.NewGuid(), repId, null);
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        // Yalnız yönetici bildirim almalı
        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(managerUserId)),
            Arg.Any<string>(),
            NotificationType.MeetingScheduled,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_LinkContainsCompanyId()
    {
        // Bildirimin Link alanı "/companies/{companyId}" formatında olmalı
        var repId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var managerUserId = Guid.NewGuid();

        var rep = MakeSalesRepWithEmployee(employeeId);
        rep.Id = repId;

        var repEmployee = new Employee("Uzman");
        _salesReps.GetByIdAsync(repId, Arg.Any<CancellationToken>()).Returns(rep);
        _employees.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(repEmployee);
        _orgScope.GetAncestorUserIdsAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { managerUserId });

        var domainEvent = new MeetingScheduledEvent(Guid.NewGuid(), companyId, repId, null);
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Any<string>(),
            Arg.Any<NotificationType>(),
            Arg.Any<string?>(),
            Arg.Is<string?>(l => l == $"/companies/{companyId}"),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}
