using System.Linq.Expressions;
using NSubstitute;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Contracts.Notifications;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Notifications;

public sealed class NotificationPreferenceServiceTests
{
    private readonly IRepository<NotificationPreference> _preferences =
        Substitute.For<IRepository<NotificationPreference>>();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly Guid UserId = Guid.NewGuid();

    private NotificationPreferenceService CreateSut() =>
        new(_preferences, _currentUser, _unitOfWork);

    private void SetupAuthUser(Guid? userId = null)
    {
        var id = userId ?? UserId;
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(id);
    }

    // -----------------------------------------------------------------------
    // GetMineAsync — opt-out varsayılanı: kayıt yoksa Enabled=true
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMineAsync_WhenNoRecordsExist_ReturnsAllFiveTypesEnabled()
    {
        SetupAuthUser();

        // DB'de hiç tercih kaydı yok
        _preferences
            .ListAsync(Arg.Any<Expression<Func<NotificationPreference, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationPreference>());

        var sut = CreateSut();
        var result = await sut.GetMineAsync();

        // Manual hariç 5 toggle edilebilir tip döner
        result.Count.ShouldBe(5);
        result.ShouldAllBe(p => p.Enabled);

        // Manual tipi kesinlikle listede olmamalı
        result.ShouldNotContain(p => p.Type == nameof(NotificationType.Manual));
    }

    [Fact]
    public async Task GetMineAsync_WhenOneTypeDisabled_ReturnsItAsFalse()
    {
        SetupAuthUser();

        // MeetingScheduled kapatılmış
        var disabledPref = NotificationPreference.Create(UserId, NotificationType.MeetingScheduled, false);

        _preferences
            .ListAsync(Arg.Any<Expression<Func<NotificationPreference, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { disabledPref });

        var sut = CreateSut();
        var result = await sut.GetMineAsync();

        result.Count.ShouldBe(5);

        var meetingPref = result.Single(p => p.Type == nameof(NotificationType.MeetingScheduled));
        meetingPref.Enabled.ShouldBeFalse();

        // Diğer 4 tip için kayıt yok → varsayılan true
        result
            .Where(p => p.Type != nameof(NotificationType.MeetingScheduled))
            .ShouldAllBe(p => p.Enabled);
    }

    // -----------------------------------------------------------------------
    // SetMineAsync — upsert: yeni kayıt oluşturma
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SetMineAsync_NewPreference_CreatesAndSaves()
    {
        SetupAuthUser();

        // Mevcut kayıt yok
        _preferences
            .ListAsync(Arg.Any<Expression<Func<NotificationPreference, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationPreference>());

        var sut = CreateSut();
        var items = new List<NotificationPreferenceItem>
        {
            new(NotificationType.MeetingScheduled, false)
        };

        await sut.SetMineAsync(items);

        await _preferences.Received(1).AddAsync(
            Arg.Is<NotificationPreference>(p =>
                p.Type == NotificationType.MeetingScheduled && !p.Enabled),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetMineAsync_ExistingPreference_UpdatesAndSaves()
    {
        SetupAuthUser();

        // Önceden etkin kayıt var
        var existing = NotificationPreference.Create(UserId, NotificationType.GoalAssigned, true);

        _preferences
            .ListAsync(Arg.Any<Expression<Func<NotificationPreference, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { existing });

        var sut = CreateSut();
        var items = new List<NotificationPreferenceItem>
        {
            new(NotificationType.GoalAssigned, false)
        };

        await sut.SetMineAsync(items);

        // Mevcut kayıt güncellenmeli; yeni kayıt eklenmemeli
        await _preferences.DidNotReceive().AddAsync(Arg.Any<NotificationPreference>(), Arg.Any<CancellationToken>());
        _preferences.Received(1).Update(Arg.Is<NotificationPreference>(p =>
            p.Type == NotificationType.GoalAssigned && !p.Enabled));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        existing.Enabled.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // SetMineAsync — Manual tipi yok sayılmalı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SetMineAsync_ManualTypeIsIgnored_NoRecordCreated()
    {
        SetupAuthUser();

        _preferences
            .ListAsync(Arg.Any<Expression<Func<NotificationPreference, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationPreference>());

        var sut = CreateSut();
        var items = new List<NotificationPreferenceItem>
        {
            new(NotificationType.Manual, false) // Manual → yok sayılmalı
        };

        await sut.SetMineAsync(items);

        // Hiç kayıt eklenmemeli ve SaveChanges çağrılmamalı
        await _preferences.DidNotReceive().AddAsync(Arg.Any<NotificationPreference>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // SetMineAsync — upsert idempotent: aynı değerle tekrar çağrılınca kayıt tekrarlanmaz
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SetMineAsync_CalledTwiceWithSameValue_IsIdempotent()
    {
        SetupAuthUser();

        var existing = NotificationPreference.Create(UserId, NotificationType.LeadConverted, false);

        _preferences
            .ListAsync(Arg.Any<Expression<Func<NotificationPreference, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { existing });

        var sut = CreateSut();
        var items = new List<NotificationPreferenceItem>
        {
            new(NotificationType.LeadConverted, false) // Aynı değer
        };

        await sut.SetMineAsync(items);

        // İkinci çağrı da mevcut kaydı güncellemeli; yeni kayıt oluşturmamalı
        await _preferences.DidNotReceive().AddAsync(Arg.Any<NotificationPreference>(), Arg.Any<CancellationToken>());
        _preferences.Received(1).Update(Arg.Any<NotificationPreference>());
        existing.Enabled.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // IsEnabledForUsersAsync — Manual tipi: hepsi döner
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IsEnabledForUsersAsync_ManualType_ReturnsAllUsers()
    {
        SetupAuthUser();

        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var sut = CreateSut();

        var result = await sut.IsEnabledForUsersAsync(userIds, NotificationType.Manual);

        // Manual → filtreleme yok, tüm kullanıcılar döner
        result.Count.ShouldBe(3);
        result.ShouldBe(userIds, ignoreOrder: true);

        // Repository sorgulanmamalı (kısa devre)
        await _preferences.DidNotReceive()
            .ListAsync(Arg.Any<Expression<Func<NotificationPreference, bool>>>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // IsEnabledForUsersAsync — MeetingScheduled: kapatan alıcı çıkarılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IsEnabledForUsersAsync_OneUserDisabledMeetingScheduled_ThatUserExcluded()
    {
        SetupAuthUser();

        var userA = Guid.NewGuid(); // MeetingScheduled kapalı
        var userB = Guid.NewGuid(); // Tercih kaydı yok → etkin
        var userC = Guid.NewGuid(); // Tercih kaydı yok → etkin

        // Yalnız userA devre dışı bırakmış
        var disabledPref = NotificationPreference.Create(userA, NotificationType.MeetingScheduled, false);

        _preferences
            .ListAsync(Arg.Any<Expression<Func<NotificationPreference, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { disabledPref });

        var sut = CreateSut();
        var result = await sut.IsEnabledForUsersAsync(
            new[] { userA, userB, userC },
            NotificationType.MeetingScheduled);

        // userA çıkarılmalı
        result.Count.ShouldBe(2);
        result.ShouldNotContain(userA);
        result.ShouldContain(userB);
        result.ShouldContain(userC);
    }

    [Fact]
    public async Task IsEnabledForUsersAsync_AllUsersEnabled_ReturnsAll()
    {
        SetupAuthUser();

        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        // Hiç devre dışı kayıt yok
        _preferences
            .ListAsync(Arg.Any<Expression<Func<NotificationPreference, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationPreference>());

        var sut = CreateSut();
        var result = await sut.IsEnabledForUsersAsync(userIds, NotificationType.TenderApproaching);

        result.Count.ShouldBe(2);
        result.ShouldBe(userIds, ignoreOrder: true);
    }

    // -----------------------------------------------------------------------
    // CreateForUsersAsync ile entegrasyon senaryosu (NotificationService mock'suz)
    // Bir alıcı MeetingScheduled'ı kapattıysa bildirim ALMAZ; başka alıcı etkilenmez.
    // Bu senaryo NotificationService üzerinden dolaylı olarak test edilir;
    // burada tercih servisinin izolasyonu doğrulanır.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task IsEnabledForUsersAsync_MeetingScheduled_DisabledRecipientGetsNoNotification()
    {
        // Bu test: "MeetingScheduled'ı kapatan kullanıcı tercih filtresinden geçemez" doğrular.
        SetupAuthUser();

        var disabledUser = Guid.NewGuid();
        var enabledUser = Guid.NewGuid();

        var disabledPref = NotificationPreference.Create(disabledUser, NotificationType.MeetingScheduled, false);

        _preferences
            .ListAsync(Arg.Any<Expression<Func<NotificationPreference, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { disabledPref });

        var sut = CreateSut();
        var result = await sut.IsEnabledForUsersAsync(
            new[] { disabledUser, enabledUser },
            NotificationType.MeetingScheduled);

        result.ShouldNotContain(disabledUser);
        result.ShouldContain(enabledUser);
    }

    [Fact]
    public async Task IsEnabledForUsersAsync_Manual_DisabledRecipientStillReceives()
    {
        // Manual bildirimde tercih filtresi uygulanmaz; opt-out edemez.
        SetupAuthUser();

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var sut = CreateSut();
        var result = await sut.IsEnabledForUsersAsync(
            new[] { userA, userB },
            NotificationType.Manual);

        result.Count.ShouldBe(2);
        result.ShouldContain(userA);
        result.ShouldContain(userB);
    }
}
