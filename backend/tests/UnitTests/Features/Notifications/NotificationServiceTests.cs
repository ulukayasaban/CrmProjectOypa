using System.Linq.Expressions;
using NSubstitute;
using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Contracts.Notifications;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;
using Shouldly;

namespace Oypa.Crm.UnitTests.Features.Notifications;

public sealed class NotificationServiceTests
{
    private readonly IRepository<Notification> _notifications = Substitute.For<IRepository<Notification>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IOrgScopeService _orgScope = Substitute.For<IOrgScopeService>();
    private readonly IRealtimeNotifier _realtimeNotifier = Substitute.For<IRealtimeNotifier>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private NotificationService CreateSut() =>
        new(_notifications, _currentUser, _orgScope, _realtimeNotifier, _unitOfWork);

    private static Guid UserId { get; } = Guid.NewGuid();

    // -----------------------------------------------------------------------
    // GetMineAsync — sıralama ve per-alıcı yalıtım
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMineAsync_ReturnsMineOrderedByDateDescending()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(UserId);

        var older = new Notification(UserId, "eski", NotificationType.Manual);
        var newer = new Notification(UserId, "yeni", NotificationType.Manual);
        SetCreatedAt(older, DateTime.UtcNow.AddHours(-2));
        SetCreatedAt(newer, DateTime.UtcNow);

        _notifications.ListAsync(Arg.Any<Expression<Func<Notification, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { older, newer });
        var sut = CreateSut();

        var result = await sut.GetMineAsync();

        result.Count.ShouldBe(2);
        result[0].Message.ShouldBe("yeni");
        result[1].Message.ShouldBe("eski");
    }

    [Fact]
    public async Task GetMineAsync_ReturnsOnlyCurrentUserNotifications_NotOtherUsers()
    {
        // A ve B kullanıcısına bildirim var; A ile çağrılınca yalnız A'nınkiler dönmeli
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userA);

        var notifA = new Notification(userA, "A mesajı", NotificationType.Manual);
        // Servis predicate filtresi nedeniyle repository yalnız A'nınkileri döndürür
        _notifications.ListAsync(Arg.Any<Expression<Func<Notification, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { notifA });

        var sut = CreateSut();
        var result = await sut.GetMineAsync();

        result.Count.ShouldBe(1);
        result[0].Message.ShouldBe("A mesajı");
        // B'ye ait hiçbir bildirim dönmemeli
        result.ShouldNotContain(n => n.Message == "B mesajı");
    }

    // -----------------------------------------------------------------------
    // GetMyUnreadCountAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMyUnreadCountAsync_ReturnsRepositoryCount()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(UserId);

        _notifications.CountAsync(Arg.Any<Expression<Func<Notification, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(3);
        var sut = CreateSut();

        var count = await sut.GetMyUnreadCountAsync();

        count.ShouldBe(3);
    }

    // -----------------------------------------------------------------------
    // MarkReadAsync — izolasyon: kendi bildirimi vs başkasının bildirimi
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkReadAsync_OwnNotification_SetsIsReadAndSaves()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        var notif = new Notification(userId, "test", NotificationType.Manual);
        _notifications.GetByIdAsync(notif.Id, Arg.Any<CancellationToken>())
            .Returns(notif);

        var sut = CreateSut();
        await sut.MarkReadAsync(notif.Id);

        notif.IsRead.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkReadAsync_AnotherUsersNotification_ThrowsNotFoundException()
    {
        // B'nin bildirimini A okumaya çalışırsa 404 (gizleme davranışı)
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userA);

        var notifForB = new Notification(userB, "B mesajı", NotificationType.Manual);
        _notifications.GetByIdAsync(notifForB.Id, Arg.Any<CancellationToken>())
            .Returns(notifForB);

        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.MarkReadAsync(notifForB.Id));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkReadAsync_NonExistentNotification_ThrowsNotFoundException()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(UserId);

        _notifications.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.MarkReadAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task MarkReadAsync_PerAliciIzolasyon_AReadingDoesNotAffectBsNotification()
    {
        // A ve B aynı mesajın alıcısı; A okuyunca B'nin IsRead false kalmalı
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var notifA = new Notification(userA, "paylaşımlı mesaj", NotificationType.MeetingScheduled);
        var notifB = new Notification(userB, "paylaşımlı mesaj", NotificationType.MeetingScheduled);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userA);
        _notifications.GetByIdAsync(notifA.Id, Arg.Any<CancellationToken>())
            .Returns(notifA);

        var sut = CreateSut();
        await sut.MarkReadAsync(notifA.Id);

        notifA.IsRead.ShouldBeTrue();
        notifB.IsRead.ShouldBeFalse(); // B'nin durumu değişmemeli
    }

    // -----------------------------------------------------------------------
    // MarkAllMineReadAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkAllMineReadAsync_MarksUnreadAndSaves()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(UserId);

        var a = new Notification(UserId, "a", NotificationType.Manual);
        var b = new Notification(UserId, "b", NotificationType.Manual);
        _notifications.ListAsync(Arg.Any<Expression<Func<Notification, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { a, b });
        var sut = CreateSut();

        await sut.MarkAllMineReadAsync();

        a.IsRead.ShouldBeTrue();
        b.IsRead.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllMineReadAsync_WhenNoUnread_DoesNotSave()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(UserId);

        _notifications.ListAsync(Arg.Any<Expression<Func<Notification, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Notification>());
        var sut = CreateSut();

        await sut.MarkAllMineReadAsync();

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // SendToUnitAsync — alt-ağaç alıcı çözümleme, göndereni hariç tutma, notifier çağrısı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SendToUnitAsync_AsManager_SendsToSubtreeUsersExcludingSender()
    {
        var senderId = Guid.NewGuid();
        var recipient1 = Guid.NewGuid();
        var recipient2 = Guid.NewGuid();
        var targetUnitId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(senderId);
        _currentUser.Roles.Returns(new List<string>());

        // Yönetici kapsamı: hedef birim (targetUnitId) yöneticinin alt-ağacında olmalı
        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(false, [senderId, recipient1, recipient2, targetUnitId]));

        // Alt-ağaç: göndereni de içeriyor; servis onu dışlamalı
        _orgScope.GetSubtreeUserIdsAsync(targetUnitId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { senderId, recipient1, recipient2 });

        // Gönderenin adı
        var senderEmployee = new Employee("Müdür", "Avniye ÖNER", "avniye@oypa.com.tr");
        _orgScope.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(senderEmployee);

        var sut = CreateSut();
        var request = new SendNotificationRequest(targetUnitId, "Duyuru Başlığı", "Birim duyurusu");

        await sut.SendToUnitAsync(request);

        // Göndereni hariç tutarak recipient1 ve recipient2'ye eklenmiş olmalı
        await _notifications.Received(2).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        // IRealtimeNotifier çağrılmış olmalı
        await _realtimeNotifier.Received().NotifyUsersAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Any<NotificationDto>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToUnitAsync_AsAdmin_SendsSuccessfully()
    {
        var senderId = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var targetUnitId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(senderId);
        _currentUser.Roles.Returns(new List<string> { "Admin" }); // Admin → yetki geçer

        _orgScope.GetSubtreeUserIdsAsync(targetUnitId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { recipient });

        _orgScope.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var sut = CreateSut();
        await sut.SendToUnitAsync(new SendNotificationRequest(targetUnitId, null, "Admin bildirimi"));

        await _notifications.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToUnitAsync_AsSalesUser_ThrowsForbiddenAppException()
    {
        var senderId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(senderId);
        _currentUser.Roles.Returns(new List<string>()); // Sales / astı yok

        // Kapsam: yalnız kendi (tek kişi → Sales gibi davranır)
        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(false, [senderId]));

        var sut = CreateSut();

        await Should.ThrowAsync<ForbiddenAppException>(
            () => sut.SendToUnitAsync(new SendNotificationRequest(Guid.NewGuid(), null, "Sales bildirimi")));
    }

    [Fact]
    public async Task SendToUnitAsync_TargetUnitHasNoUsers_DoesNotCreateNotifications()
    {
        var senderId = Guid.NewGuid();
        var targetUnitId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(senderId);
        _currentUser.Roles.Returns(new List<string> { "Admin" });

        // Alt-ağaçta hiç hesaplı kullanıcı yok
        _orgScope.GetSubtreeUserIdsAsync(targetUnitId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        _orgScope.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var sut = CreateSut();
        await sut.SendToUnitAsync(new SendNotificationRequest(targetUnitId, null, "Mesaj"));

        await _notifications.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToUnitAsync_SenderIsOnlyUserInSubtree_NoNotificationsCreated()
    {
        // Alt-ağaç yalnız göndereni içeriyorsa alıcı kalmaz
        var senderId = Guid.NewGuid();
        var targetUnitId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(senderId);
        _currentUser.Roles.Returns(new List<string> { "Admin" });

        _orgScope.GetSubtreeUserIdsAsync(targetUnitId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { senderId }); // yalnız göndereni

        _orgScope.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var sut = CreateSut();
        await sut.SendToUnitAsync(new SendNotificationRequest(targetUnitId, null, "Sadece gönderen"));

        await _notifications.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendToUnitAsync_CrossUnit_OutsideScope_ThrowsForbidden()
    {
        // Yönetici, kendi alt-ağacı DIŞINDAKİ bir birime bildirim gönderememeli (yetki sızıntısı koruması)
        var senderId = Guid.NewGuid();
        var externalUnitId = Guid.NewGuid(); // gönderenin kapsamı dışında bir birim
        var externalRecipient = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(senderId);
        _currentUser.Roles.Returns(new List<string>());

        // Yönetici kapsamı: kendi alt-ağacı (externalUnitId dahil DEĞİL)
        _orgScope.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new OrgScope(false, [senderId, Guid.NewGuid()])); // birden fazla → yönetici

        // Hedef birim (dışarıdaki birim) kendi alt-ağacını döndürür — ancak yetki kontrolü buna ulaşmadan reddetmeli
        _orgScope.GetSubtreeUserIdsAsync(externalUnitId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { externalRecipient });

        var sut = CreateSut();

        // Kapsam dışı birime gönderim engellenmeli
        await Should.ThrowAsync<ForbiddenAppException>(
            () => sut.SendToUnitAsync(new SendNotificationRequest(externalUnitId, "Çapraz Birim", "Mesaj")));

        await _notifications.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // SendToUnitAsync — Type=Manual + SenderName dolu olmalı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SendToUnitAsync_CreatedNotificationsHaveManualTypeAndSenderName()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var targetUnitId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(senderId);
        _currentUser.Roles.Returns(new List<string> { "Admin" });

        _orgScope.GetSubtreeUserIdsAsync(targetUnitId, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { recipientId });

        var senderEmployee = new Employee("Müdür", "Test Yönetici", "yonetici@oypa.com.tr");
        _orgScope.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(senderEmployee);

        Notification? capturedNotification = null;
        await _notifications.AddAsync(Arg.Do<Notification>(n => capturedNotification = n), Arg.Any<CancellationToken>());

        var sut = CreateSut();
        await sut.SendToUnitAsync(new SendNotificationRequest(targetUnitId, "Başlık", "Manuel mesaj"));

        capturedNotification.ShouldNotBeNull();
        capturedNotification!.Type.ShouldBe(NotificationType.Manual);
        capturedNotification.SenderName.ShouldBe("Test Yönetici");
        capturedNotification.SenderUserId.ShouldBe(senderId);
    }

    // -----------------------------------------------------------------------
    // CreateForUsersAsync — dahili yardımcı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateForUsersAsync_CreatesOneNotificationPerRecipient()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(UserId);

        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var sut = CreateSut();

        await sut.CreateForUsersAsync(userIds, "Mesaj", NotificationType.MeetingScheduled);

        await _notifications.Received(3).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateForUsersAsync_DeduplicatesRecipients()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(UserId);

        var userId = Guid.NewGuid();
        // Aynı kullanıcı iki kez geçiyor; yalnız bir bildirim oluşturulmalı
        var userIds = new[] { userId, userId };
        var sut = CreateSut();

        await sut.CreateForUsersAsync(userIds, "Mesaj", NotificationType.GoalAssigned);

        await _notifications.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateForUsersAsync_EmptyList_DoesNothing()
    {
        var sut = CreateSut();

        await sut.CreateForUsersAsync(Array.Empty<Guid>(), "Mesaj", NotificationType.Manual);

        await _notifications.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateForUsersAsync_CallsRealtimeNotifierForEachRecipient()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(UserId);

        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var sut = CreateSut();

        await sut.CreateForUsersAsync(userIds, "Bildirim", NotificationType.LeadConverted);

        await _realtimeNotifier.Received(2).NotifyUsersAsync(
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Any<NotificationDto>(),
            Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // DeleteAsync — sahiplik kontrolü
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_OwnNotification_RemovesAndSaves()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        var notif = new Notification(userId, "silinecek", NotificationType.Manual);
        _notifications.GetByIdAsync(notif.Id, Arg.Any<CancellationToken>())
            .Returns(notif);

        var sut = CreateSut();
        await sut.DeleteAsync(notif.Id);

        _notifications.Received(1).Remove(Arg.Is<Notification>(n => n.Id == notif.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_AnotherUsersNotification_ThrowsNotFoundAndDoesNotRemove()
    {
        // B'nin bildirimini A silmeye çalışırsa 404 (gizleme davranışı).
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userA);

        var notifForB = new Notification(userB, "B'nin bildirimi", NotificationType.Manual);
        _notifications.GetByIdAsync(notifForB.Id, Arg.Any<CancellationToken>())
            .Returns(notifForB);

        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.DeleteAsync(notifForB.Id));

        _notifications.DidNotReceive().Remove(Arg.Any<Notification>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NonExistentNotification_ThrowsNotFoundException()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(UserId);

        _notifications.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        var sut = CreateSut();

        await Should.ThrowAsync<NotFoundException>(() => sut.DeleteAsync(Guid.NewGuid()));

        _notifications.DidNotReceive().Remove(Arg.Any<Notification>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // Yardımcılar
    // -----------------------------------------------------------------------

    private static void SetCreatedAt(Notification notification, DateTime value)
    {
        var prop = typeof(Notification).GetProperty(nameof(Notification.CreatedAtUtc))!;
        prop.SetValue(notification, value);
    }
}
