using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Meetings;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Contracts.Meetings;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Meetings;

public sealed class MeetingServiceTests
{
    private readonly IMeetingRepository _meetings = Substitute.For<IMeetingRepository>();
    private readonly IRepository<Company> _companies = Substitute.For<IRepository<Company>>();
    private readonly IRepository<SalesRep> _salesReps = Substitute.For<IRepository<SalesRep>>();
    private readonly IRepository<Contact> _contacts = Substitute.For<IRepository<Contact>>();
    private readonly IRepository<Employee> _employees = Substitute.For<IRepository<Employee>>();
    private readonly IRepository<MailDraft> _mailDrafts = Substitute.For<IRepository<MailDraft>>();
    private readonly IOrgScopeService _orgScope = Substitute.For<IOrgScopeService>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private MeetingService CreateSut() =>
        new(_meetings, _companies, _salesReps, _contacts, _employees, _mailDrafts,
            _orgScope, _notificationService, _currentUser, _identityService, _unitOfWork);

    private static ScheduleMeetingRequest Request(Guid companyId, Guid repId, Guid? contactId = null) =>
        new(companyId, contactId, repId,
            new DateOnly(2026, 6, 8), new TimeOnly(10, 0), "Adres", MeetingMethod.Visit);

    [Fact]
    public async Task ScheduleAsync_CompanyNotFound_ThrowsNotFound()
    {
        _companies.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Company?)null);
        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(
            () => sut.ScheduleAsync(Request(Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task ScheduleAsync_ContactBelongsToAnotherCompany_ThrowsConflict()
    {
        var company = new Company("Acme", Sector.Retail, "1", "a@b.c", "Adr");
        var rep = new SalesRep("Rep", "rep@oypa.com");
        var contact = new Contact(Guid.NewGuid(), "Foreign", null, null); // different companyId

        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(rep.Id, Arg.Any<CancellationToken>()).Returns(rep);
        _contacts.GetByIdAsync(contact.Id, Arg.Any<CancellationToken>()).Returns(contact);
        var sut = CreateSut();

        await Should.ThrowAsync<ConflictException>(
            () => sut.ScheduleAsync(Request(company.Id, rep.Id, contact.Id)));
    }

    [Fact]
    public async Task ScheduleAsync_Valid_AddsMailDraftAndSaves()
    {
        var company = new Company("Acme", Sector.Retail, "1", "a@b.c", "Adr");
        var rep = new SalesRep("Rep", "rep@oypa.com");
        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(rep.Id, Arg.Any<CancellationToken>()).Returns(rep);
        var sut = CreateSut();

        var result = await sut.ScheduleAsync(Request(company.Id, rep.Id));

        result.CompanyId.ShouldBe(company.Id);
        result.Status.ShouldBe(MeetingStatus.Planned);
        await _meetings.Received(1).AddAsync(Arg.Any<Meeting>(), Arg.Any<CancellationToken>());
        await _mailDrafts.Received(1).AddAsync(Arg.Any<MailDraft>(), Arg.Any<CancellationToken>());
        // Bildirim artık MeetingScheduledNotificationHandler (event) üzerinden üretilir;
        // burada INotificationService doğrudan çağrılmaz.
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_PlannedStatus_ThrowsConflict()
    {
        var meeting = Meeting.Schedule(
            Guid.NewGuid(), Guid.NewGuid(), null,
            new DateOnly(2026, 6, 8), new TimeOnly(10, 0), "Adr", MeetingMethod.Visit);
        _meetings.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        var sut = CreateSut();

        await Should.ThrowAsync<ConflictException>(
            () => sut.UpdateStatusAsync(meeting.Id, new UpdateMeetingStatusRequest(MeetingStatus.Planned, null)));
    }

    [Fact]
    public async Task ScheduleAsync_WithContact_CreatedMailDraftCcIsContactEmail()
    {
        var company = new Company("Acme", Sector.Retail, "1", "a@b.c", "Adr");
        var rep = new SalesRep("Rep", "rep@oypa.com");
        var contact = new Contact(company.Id, "Kisi", "kisi@firm.com", "555");

        _companies.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);
        _salesReps.GetByIdAsync(rep.Id, Arg.Any<CancellationToken>()).Returns(rep);
        _contacts.GetByIdAsync(contact.Id, Arg.Any<CancellationToken>()).Returns(contact);
        var sut = CreateSut();

        await sut.ScheduleAsync(Request(company.Id, rep.Id, contact.Id));

        await _mailDrafts.Received(1).AddAsync(
            Arg.Is<MailDraft>(d => d.Cc == contact.Email && d.To == rep.Email),
            Arg.Any<CancellationToken>());
    }
}
