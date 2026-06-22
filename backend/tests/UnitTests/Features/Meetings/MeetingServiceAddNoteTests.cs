using System.Linq.Expressions;
using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Common.Models;
using Oypa.Crm.Application.Features.Meetings;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Contracts.Meetings;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Meetings;

/// <summary>
/// MeetingService.AddNoteAsync kapsamını genişleten birim testleri.
/// </summary>
public sealed class MeetingServiceAddNoteTests
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

    private static Meeting MakeMeeting(Guid? companyId = null)
    {
        var cId = companyId ?? Guid.NewGuid();
        return Meeting.Schedule(
            cId, Guid.NewGuid(), null,
            new DateOnly(2026, 6, 11), new TimeOnly(10, 0), "Adres", MeetingMethod.Visit);
    }

    // -----------------------------------------------------------------------
    // Hata durumları
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddNoteAsync_UnknownMeetingId_ThrowsNotFoundException()
    {
        _meetings.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Meeting?)null);

        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(
            () => sut.AddNoteAsync(Guid.NewGuid(), new AddMeetingNoteRequest("İçerik")));
    }

    // -----------------------------------------------------------------------
    // Yazar çözümleme — currentUser.UserId null
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddNoteAsync_NullUserId_AuthorNameIsSystem()
    {
        var meeting = MakeMeeting();
        _currentUser.UserId.Returns((Guid?)null);
        _meetings.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _meetings.ListByCompanyWithDetailsAsync(meeting.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });

        // SalesRep araması için boş liste döndür
        _salesReps.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SalesRep?)null);

        var sut = CreateSut();

        var dto = await sut.AddNoteAsync(meeting.Id, new AddMeetingNoteRequest("Sistem notu"));

        dto.Notes.ShouldNotBeEmpty();
        dto.Notes[0].AuthorName.ShouldBe("Sistem");
        dto.Notes[0].AuthorTitle.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // Yazar çözümleme — Position dolu
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddNoteAsync_UserHasPosition_AuthorTitleIsPositionValue()
    {
        var userId = Guid.NewGuid();
        var meeting = MakeMeeting();

        _currentUser.UserId.Returns(userId);
        _identityService.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AuthUserInfo(userId, "a@b.c", "Umur KUTLU", "Pazarlama Direktörü", null, []));

        _meetings.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _meetings.ListByCompanyWithDetailsAsync(meeting.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });
        _salesReps.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SalesRep?)null);

        var sut = CreateSut();

        var dto = await sut.AddNoteAsync(meeting.Id, new AddMeetingNoteRequest("Direktör notu"));

        dto.Notes[0].AuthorName.ShouldBe("Umur KUTLU");
        dto.Notes[0].AuthorTitle.ShouldBe("Pazarlama Direktörü");
    }

    // -----------------------------------------------------------------------
    // Yazar çözümleme — Position boş, Employee.Title fallback
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddNoteAsync_EmptyPosition_FallsBackToEmployeeTitle()
    {
        var userId = Guid.NewGuid();
        var meeting = MakeMeeting();

        _currentUser.UserId.Returns(userId);
        _identityService.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AuthUserInfo(userId, "b@c.d", "Muhammed MARANGOZ", null /* Position boş */, null, []));

        var employee = new Employee("Satış Uzmanı", "Muhammed MARANGOZ", "b@c.d");
        employee.LinkAccount(userId);

        _employees.ListAsync(
            Arg.Any<Expression<Func<Employee, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(new[] { employee });

        _meetings.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _meetings.ListByCompanyWithDetailsAsync(meeting.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });
        _salesReps.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SalesRep?)null);

        var sut = CreateSut();

        var dto = await sut.AddNoteAsync(meeting.Id, new AddMeetingNoteRequest("Uzman notu"));

        dto.Notes[0].AuthorTitle.ShouldBe("Satış Uzmanı");
    }

    // -----------------------------------------------------------------------
    // Dönen DTO — not içeriği ve SaveChanges
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddNoteAsync_Valid_SavesAndReturnsUpdatedMeetingDto()
    {
        var userId = Guid.NewGuid();
        var meeting = MakeMeeting();

        _currentUser.UserId.Returns(userId);
        _identityService.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AuthUserInfo(userId, "x@y.z", "Avniye ÖNER", "Satış Müdürü", null, []));

        _meetings.GetByIdAsync(meeting.Id, Arg.Any<CancellationToken>()).Returns(meeting);
        _meetings.ListByCompanyWithDetailsAsync(meeting.CompanyId, Arg.Any<CancellationToken>())
            .Returns(new[] { meeting });
        _salesReps.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SalesRep?)null);

        var sut = CreateSut();

        var dto = await sut.AddNoteAsync(meeting.Id, new AddMeetingNoteRequest("Not içeriği"));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        dto.Id.ShouldBe(meeting.Id);
        dto.Notes.ShouldNotBeEmpty();
        dto.Notes[0].Content.ShouldBe("Not içeriği");
    }
}
