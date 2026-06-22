using Microsoft.Extensions.Logging;
using NSubstitute;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Application.Features.Tenders;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Tenders;

public sealed class TenderReminderServiceTests
{
    private readonly ITenderRepository _tenders = Substitute.For<ITenderRepository>();
    private readonly IRepository<Employee> _employees = Substitute.For<IRepository<Employee>>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<TenderReminderService> _logger = Substitute.For<ILogger<TenderReminderService>>();

    private static readonly DateOnly Today = new(2026, 6, 11);
    private static readonly DateTime UtcNow = new(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc);

    private TenderReminderService CreateSut()
    {
        _clock.Today.Returns(Today);
        _clock.UtcNow.Returns(UtcNow);
        return new(_tenders, _employees, _notificationService, _clock, _unitOfWork, _logger);
    }

    /// <summary>
    /// SalesRep ile bağlantılı bir Employee ve ApplicationUserId oluşturur.
    /// SalesRep.Employee navigation'ı reflection ile set edilir.
    /// </summary>
    private static (Tender tender, SalesRep rep, Employee employee, Guid userId) BuildTenderWithRep(
        DateOnly tenderDate,
        TenderStatus status = TenderStatus.Hazirlik,
        bool hasEmployeeId = true,
        bool hasUserId = true)
    {
        var userId = Guid.NewGuid();
        var employee = new Employee("Temsilci", "Ad Soyad", "temsilci@oypa.com");
        if (hasUserId)
            employee.LinkAccount(userId);

        var rep = new SalesRep("Test Temsilci", "temsilci@oypa.com");
        if (hasEmployeeId)
        {
            // EmployeeId private setter — reflection ile set et
            typeof(SalesRep)
                .GetProperty(nameof(SalesRep.EmployeeId))!
                .SetValue(rep, employee.Id);
            typeof(SalesRep)
                .GetProperty(nameof(SalesRep.Employee))!
                .SetValue(rep, employee);
        }

        var company = new Company("Firma A", Sector.Retail, "111", "a@b.c", "Adr");
        var tender = Tender.Create(
            company.Id, "Test İhalesi", null, Sector.Retail,
            tenderDate, null, null, null, null, null, rep.Id);

        // AssignedSalesRep navigation'ı reflection ile set et
        typeof(Tender)
            .GetProperty(nameof(Tender.AssignedSalesRep))!
            .SetValue(tender, rep);

        // Company navigation
        typeof(Tender)
            .GetProperty(nameof(Tender.Company))!
            .SetValue(tender, company);

        // Status set et
        if (status != TenderStatus.Hazirlik)
            tender.ChangeStatus(status);

        return (tender, rep, employee, userId);
    }

    // -----------------------------------------------------------------------
    // Aktif + aralıkta + ApproachNotifiedAtUtc null → bildirim gönderilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task NotifyApproachingAsync_ActiveTenderWithinRange_SendsNotificationAndMarksNotified()
    {
        var tenderDate = Today.AddDays(3); // aralıkta
        var (tender, rep, employee, userId) = BuildTenderWithRep(tenderDate);

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var sut = CreateSut();

        var count = await sut.NotifyApproachingAsync(7);

        count.ShouldBe(1);
        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(userId)),
            Arg.Any<string>(),
            NotificationType.TenderApproaching,
            title: "Yaklaşan İhale",
            link: "/tenders/aktif",
            cancellationToken: Arg.Any<CancellationToken>());
        tender.ApproachNotifiedAtUtc.ShouldNotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyApproachingAsync_TeklifVerildiStatus_SendsNotification()
    {
        var tenderDate = Today.AddDays(5);
        var (tender, _, employee, userId) = BuildTenderWithRep(tenderDate, TenderStatus.TeklifVerildi);

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var sut = CreateSut();

        var count = await sut.NotifyApproachingAsync(7);

        count.ShouldBe(1);
        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Any<string>(),
            NotificationType.TenderApproaching,
            title: Arg.Any<string?>(),
            link: Arg.Any<string?>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Repository filtresi dışında kalan ihaleler → bildirim yok
    // (Kapsam dışı senaryolar — repository bunları ListApproachingAsync'e dahil etmez)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task NotifyApproachingAsync_EmptyApproachingList_ReturnsZero()
    {
        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Tender>());
        var sut = CreateSut();

        var count = await sut.NotifyApproachingAsync(7);

        count.ShouldBe(0);
        await _notificationService.DidNotReceive()
            .CreateForUsersAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(),
                Arg.Any<NotificationType>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // AssignedSalesRep yok veya EmployeeId yok → atlanır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task NotifyApproachingAsync_SalesRepHasNoEmployeeId_SkipsTender()
    {
        var tenderDate = Today.AddDays(3);
        var (tender, _, employee, _) = BuildTenderWithRep(tenderDate, hasEmployeeId: false);

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        var sut = CreateSut();

        var count = await sut.NotifyApproachingAsync(7);

        count.ShouldBe(0);
        await _notificationService.DidNotReceive()
            .CreateForUsersAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(),
                Arg.Any<NotificationType>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyApproachingAsync_EmployeeHasNoApplicationUserId_SkipsTender()
    {
        var tenderDate = Today.AddDays(3);
        var (tender, _, employee, _) = BuildTenderWithRep(tenderDate, hasUserId: false);

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var sut = CreateSut();

        var count = await sut.NotifyApproachingAsync(7);

        count.ShouldBe(0);
        await _notificationService.DidNotReceive()
            .CreateForUsersAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(),
                Arg.Any<NotificationType>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyApproachingAsync_EmployeeNotFoundInRepo_SkipsTender()
    {
        var tenderDate = Today.AddDays(3);
        var (tender, _, employee, _) = BuildTenderWithRep(tenderDate);

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);
        var sut = CreateSut();

        var count = await sut.NotifyApproachingAsync(7);

        count.ShouldBe(0);
    }

    // -----------------------------------------------------------------------
    // İDEMPOTENT — ikinci çağrıda yeni bildirim üretilmez
    // -----------------------------------------------------------------------

    [Fact]
    public async Task NotifyApproachingAsync_Idempotent_SecondCallDoesNotSendNewNotification()
    {
        var tenderDate = Today.AddDays(3);
        var (tender, _, employee, userId) = BuildTenderWithRep(tenderDate);

        // İlk çağrı: ihale döndürülür
        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var sut = CreateSut();

        await sut.NotifyApproachingAsync(7);

        // MarkApproachNotified çağrıldıktan sonra ApproachNotifiedAtUtc dolu olur;
        // ikinci çağrıda repository bu ihaleyi artık döndürmez (ListApproachingAsync filtresi ApproachNotifiedAtUtc null şartı).
        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Tender>()); // idempotent: null olmayan → filtre dışı

        var secondCount = await sut.NotifyApproachingAsync(7);

        secondCount.ShouldBe(0);
        // Toplam bildirim gönderimi yalnızca 1 kez gerçekleşmeli
        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(),
            NotificationType.TenderApproaching,
            title: Arg.Any<string?>(), link: Arg.Any<string?>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyApproachingAsync_FirstCall_SetsApproachNotifiedAtUtc()
    {
        var tenderDate = Today.AddDays(3);
        var (tender, _, employee, _) = BuildTenderWithRep(tenderDate);

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var sut = CreateSut();

        tender.ApproachNotifiedAtUtc.ShouldBeNull();

        await sut.NotifyApproachingAsync(7);

        tender.ApproachNotifiedAtUtc.ShouldNotBeNull();
        tender.ApproachNotifiedAtUtc.ShouldBe(UtcNow);
    }

    // -----------------------------------------------------------------------
    // Bildirim mesajı firma adı + ihale başlığı içerir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task NotifyApproachingAsync_Message_ContainsCompanyTitleAndTenderTitle()
    {
        var tenderDate = Today.AddDays(3);
        var (tender, _, employee, _) = BuildTenderWithRep(tenderDate);

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var sut = CreateSut();

        string? capturedMessage = null;
        await _notificationService.CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Do<string>(m => capturedMessage = m),
            Arg.Any<NotificationType>(),
            title: Arg.Any<string?>(),
            link: Arg.Any<string?>(),
            cancellationToken: Arg.Any<CancellationToken>());

        await sut.NotifyApproachingAsync(7);

        capturedMessage.ShouldNotBeNull();
        capturedMessage.ShouldContain("Firma A");
        capturedMessage.ShouldContain("Test İhalesi");
    }

    // -----------------------------------------------------------------------
    // Birden fazla ihale: her biri bağımsız işlenir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task NotifyApproachingAsync_MultipleActiveTenders_SendsOneNotificationEach()
    {
        var (t1, _, e1, u1) = BuildTenderWithRep(Today.AddDays(2));
        var (t2, _, e2, u2) = BuildTenderWithRep(Today.AddDays(5));

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { t1, t2 });
        _employees.GetByIdAsync(e1.Id, Arg.Any<CancellationToken>()).Returns(e1);
        _employees.GetByIdAsync(e2.Id, Arg.Any<CancellationToken>()).Returns(e2);
        var sut = CreateSut();

        var count = await sut.NotifyApproachingAsync(7);

        count.ShouldBe(2);
        await _notificationService.Received(2).CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(),
            NotificationType.TenderApproaching,
            title: Arg.Any<string?>(), link: Arg.Any<string?>(),
            cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyApproachingAsync_OneTenderValidOneSkipped_CountsOnlyValid()
    {
        // t1 geçerli (Employee + UserId var), t2 Employee'siz
        var (t1, _, e1, u1) = BuildTenderWithRep(Today.AddDays(2));
        var (t2, _, _, _) = BuildTenderWithRep(Today.AddDays(4), hasEmployeeId: false);

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { t1, t2 });
        _employees.GetByIdAsync(e1.Id, Arg.Any<CancellationToken>()).Returns(e1);
        var sut = CreateSut();

        var count = await sut.NotifyApproachingAsync(7);

        count.ShouldBe(1);
        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(),
            NotificationType.TenderApproaching,
            title: Arg.Any<string?>(), link: Arg.Any<string?>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // SaveChanges yalnızca işlenmiş ihale varsa çağrılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task NotifyApproachingAsync_NoTendersProcessed_DoesNotCallSaveChanges()
    {
        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Tender>());
        var sut = CreateSut();

        await sut.NotifyApproachingAsync(7);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Bildirim tipi TenderApproaching, link /tenders/aktif
    // -----------------------------------------------------------------------

    [Fact]
    public async Task NotifyApproachingAsync_CreatedNotification_HasCorrectTypeAndLink()
    {
        var tenderDate = Today.AddDays(3);
        var (tender, _, employee, userId) = BuildTenderWithRep(tenderDate);

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var sut = CreateSut();

        await sut.NotifyApproachingAsync(7);

        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(userId)),
            Arg.Any<string>(),
            NotificationType.TenderApproaching,
            title: "Yaklaşan İhale",
            link: "/tenders/aktif",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Yalnız atanan sorumluya bildirim gönderilir (tek alıcı)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task NotifyApproachingAsync_OnlyAssignedRepReceivesNotification()
    {
        var tenderDate = Today.AddDays(3);
        var (tender, _, employee, userId) = BuildTenderWithRep(tenderDate);

        _tenders.ListApproachingAsync(Today, 7, Arg.Any<CancellationToken>())
            .Returns(new[] { tender });
        _employees.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        var sut = CreateSut();

        IEnumerable<Guid>? capturedIds = null;
        await _notificationService.CreateForUsersAsync(
            Arg.Do<IEnumerable<Guid>>(ids => capturedIds = ids),
            Arg.Any<string>(), Arg.Any<NotificationType>(),
            title: Arg.Any<string?>(), link: Arg.Any<string?>(),
            cancellationToken: Arg.Any<CancellationToken>());

        await sut.NotifyApproachingAsync(7);

        capturedIds.ShouldNotBeNull();
        var idList = capturedIds!.ToList();
        idList.Count.ShouldBe(1);
        idList[0].ShouldBe(userId);
    }
}
