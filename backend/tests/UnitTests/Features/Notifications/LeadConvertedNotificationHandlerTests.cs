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
/// LeadConvertedNotificationHandler birim testleri:
/// lead müşteriye dönüştürüldüğünde atanan SalesRep'in yönetici zincirine bildirim üretilir.
/// </summary>
public sealed class LeadConvertedNotificationHandlerTests
{
    private readonly IOrgScopeService _orgScope = Substitute.For<IOrgScopeService>();
    private readonly IRepository<Company> _companies = Substitute.For<IRepository<Company>>();
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ILogger<LeadConvertedNotificationHandler> _logger =
        Substitute.For<ILogger<LeadConvertedNotificationHandler>>();

    private LeadConvertedNotificationHandler CreateSut() =>
        new(_orgScope, _companies, _salesReps, _notificationService, _logger);

    private static SalesRep MakeSalesRepWithEmployee(Guid employeeId)
    {
        var rep = new SalesRep("Temsilci", "rep@oypa.com");
        rep.LinkEmployee(employeeId);
        return rep;
    }

    // -----------------------------------------------------------------------
    // Mutlu yol
    // -----------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_CompanyWithAssignedRepAndManagerChain_CreatesNotifications()
    {
        var companyId = Guid.NewGuid();
        var repId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var managerUserId = Guid.NewGuid();

        var company = new Company("Acme A.Ş.", Sector.Retail, "0212", "acme@a.com", "Adres");
        company.AssignSalesRep(repId);

        var rep = MakeSalesRepWithEmployee(employeeId);
        rep.Id = repId;

        _companies.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(repId, Arg.Any<CancellationToken>()).Returns(rep);
        _orgScope.GetAncestorUserIdsAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { managerUserId });

        var domainEvent = new LeadConvertedToCustomerEvent(companyId, "Acme A.Ş.");
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(managerUserId)),
            Arg.Is<string>(m => m.Contains("Acme A.Ş.")),
            NotificationType.LeadConverted,
            Arg.Any<string?>(),
            Arg.Is<string?>(l => l == $"/companies/{companyId}"),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Edge case'ler: erken çıkış senaryoları
    // -----------------------------------------------------------------------

    [Fact]
    public async Task HandleAsync_CompanyNotFound_DoesNotCreateNotifications()
    {
        _companies.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Company?)null);

        var domainEvent = new LeadConvertedToCustomerEvent(Guid.NewGuid(), "Bilinmeyen Firma");
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        await _notificationService.DidNotReceive().CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CompanyWithNoAssignedRep_DoesNotCreateNotifications()
    {
        var companyId = Guid.NewGuid();
        var company = new Company("Firma", Sector.Energy, "0212", "f@f.com", "Adres");
        // AssignedSalesRepId set edilmemiş

        _companies.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);

        var domainEvent = new LeadConvertedToCustomerEvent(companyId, "Firma");
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        await _notificationService.DidNotReceive().CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RepWithNoEmployeeId_DoesNotCreateNotifications()
    {
        var companyId = Guid.NewGuid();
        var repId = Guid.NewGuid();

        var company = new Company("Firma", Sector.Retail, "0212", "f@f.com", "Adres");
        company.AssignSalesRep(repId);

        var repWithNoEmployee = new SalesRep("Temsilci", "rep@oypa.com"); // EmployeeId = null
        repWithNoEmployee.Id = repId;

        _companies.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(repId, Arg.Any<CancellationToken>()).Returns(repWithNoEmployee);

        var domainEvent = new LeadConvertedToCustomerEvent(companyId, "Firma");
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        await _notificationService.DidNotReceive().CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmptyAncestorChain_DoesNotCreateNotifications()
    {
        // Yönetici zinciri boşsa bildirim üretilmemeli
        var companyId = Guid.NewGuid();
        var repId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var company = new Company("Firma", Sector.Energy, "0212", "f@f.com", "Adres");
        company.AssignSalesRep(repId);

        var rep = MakeSalesRepWithEmployee(employeeId);
        rep.Id = repId;

        _companies.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(repId, Arg.Any<CancellationToken>()).Returns(rep);
        _orgScope.GetAncestorUserIdsAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>()); // Boş zincir

        var domainEvent = new LeadConvertedToCustomerEvent(companyId, "Firma");
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        await _notificationService.DidNotReceive().CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MessageContainsCompanyTitle()
    {
        var companyId = Guid.NewGuid();
        var repId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var managerUserId = Guid.NewGuid();
        const string companyTitle = "OYPA Test A.Ş.";

        var company = new Company(companyTitle, Sector.Retail, "0212", "oypa@a.com", "Adres");
        company.AssignSalesRep(repId);

        var rep = MakeSalesRepWithEmployee(employeeId);
        rep.Id = repId;

        _companies.GetByIdAsync(companyId, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(repId, Arg.Any<CancellationToken>()).Returns(rep);
        _orgScope.GetAncestorUserIdsAsync(employeeId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { managerUserId });

        var domainEvent = new LeadConvertedToCustomerEvent(companyId, companyTitle);
        var sut = CreateSut();

        await sut.HandleAsync(domainEvent);

        await _notificationService.Received(1).CreateForUsersAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Is<string>(m => m.Contains(companyTitle)),
            Arg.Any<NotificationType>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}
