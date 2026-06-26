using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Features.Notifications;
using Oypa.Crm.Application.Mappings;
using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Meetings;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Meetings;

public sealed class MeetingService(
    IMeetingRepository meetings,
    IRepository<Company> companies,
    IRepository<SalesRep> salesReps,
    IRepository<Contact> contacts,
    IRepository<Employee> employees,
    IRepository<MailDraft> mailDrafts,
    IOrgScopeService orgScope,
    INotificationService notificationService,
    ICurrentUser currentUser,
    IIdentityService identityService,
    IUnitOfWork unitOfWork) : IMeetingService
{
    public async Task<IReadOnlyList<MeetingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await meetings.ListWithDetailsAsync(cancellationToken);
        return list.Select(m => m.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<MeetingDto>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = await meetings.ListByCompanyWithDetailsAsync(companyId, cancellationToken);
        return list.Select(m => m.ToDto()).ToList();
    }

    public async Task<MeetingDto> ScheduleAsync(ScheduleMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var company = await companies.GetByIdAsync(request.CompanyId, cancellationToken)
                      ?? throw NotFoundException.For("Firma", request.CompanyId);

        var rep = await salesReps.GetByIdAsync(request.SalesRepId, cancellationToken)
                  ?? throw NotFoundException.For("Satış temsilcisi", request.SalesRepId);

        Contact? contact = null;
        if (request.ContactId is { } contactId)
        {
            contact = await contacts.GetByIdAsync(contactId, cancellationToken)
                      ?? throw NotFoundException.For("İlgili kişi", contactId);

            if (contact.CompanyId != company.Id)
                throw new ConflictException("Seçilen ilgili kişi bu firmaya ait değil.");
        }

        // Meeting.Schedule() MeetingScheduledEvent tetikler; event handler notification üretir.
        var meeting = Meeting.Schedule(company.Id, rep.Id, contact?.Id, request.Date, request.Time, request.Address, request.Method);
        await meetings.AddAsync(meeting, cancellationToken);

        var draft = BuildReminderDraft(meeting, company, rep, contact);
        await mailDrafts.AddAsync(draft, cancellationToken);

        // Görüşme oluşturulduğunda firmada etkileşim kaydı güncellenir (pasifse aktife döner).
        company.RegisterInteraction(DateTime.UtcNow, reactivate: true);

        // SaveChangesAsync domain event dispatch'i de yapar → MeetingScheduledNotificationHandler çalışır.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new MeetingDto(
            meeting.Id, company.Id, company.Title, contact?.Id, contact?.Name,
            rep.Id, rep.Name, rep.Employee?.Title,
            meeting.Date, meeting.Time, meeting.Address,
            meeting.Method, meeting.Status, meeting.Comment,
            []);
    }

    public async Task UpdateStatusAsync(Guid id, UpdateMeetingStatusRequest request, CancellationToken cancellationToken = default)
    {
        var meeting = await meetings.GetByIdAsync(id, cancellationToken)
                      ?? throw NotFoundException.For("Görüşme", id);

        switch (request.Status)
        {
            case MeetingStatus.Done:
                meeting.MarkAsDone(request.Comment);
                break;
            case MeetingStatus.Cancelled:
                meeting.Cancel(request.Comment);
                break;
            default:
                throw new ConflictException("Görüşme yalnızca 'Yapıldı' veya 'İptal' durumuna güncellenebilir.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MeetingDto> AddNoteAsync(
        Guid meetingId,
        AddMeetingNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var meeting = await meetings.GetByIdAsync(meetingId, cancellationToken)
                      ?? throw NotFoundException.For("Görüşme", meetingId);

        var (authorName, authorTitle) = await ResolveAuthorAsync(cancellationToken);

        meeting.AddNote(request.Content, currentUser.UserId, authorName, authorTitle);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Not yazarının yönetici zincirine bildirim gönder (yazar hariç)
        await NotifyNoteAddedAsync(meeting, cancellationToken);

        var updated = (await meetings.ListByCompanyWithDetailsAsync(meeting.CompanyId, cancellationToken))
            .FirstOrDefault(m => m.Id == meetingId)
            ?? throw NotFoundException.For("Görüşme", meetingId);

        return updated.ToDto();
    }

    /// <summary>
    /// Görüşmenin SalesRep'inin yönetici zincirine not eklendiğini bildirir.
    /// Not yazarı bildirim almaz.
    /// </summary>
    private async Task NotifyNoteAddedAsync(Meeting meeting, CancellationToken cancellationToken)
    {
        var rep = await salesReps.GetByIdAsync(meeting.SalesRepId, cancellationToken);
        if (rep?.EmployeeId is null)
            return;

        var ancestorUserIds = await orgScope.GetAncestorUserIdsAsync(rep.EmployeeId.Value, cancellationToken);

        // Not yazan kullanıcı hariç tut
        var recipients = currentUser.UserId.HasValue
            ? ancestorUserIds.Where(uid => uid != currentUser.UserId.Value)
            : ancestorUserIds;

        var company = await companies.GetByIdAsync(meeting.CompanyId, cancellationToken);
        var companyTitle = company?.Title ?? "Firma";

        await notificationService.CreateForUsersAsync(
            recipients,
            $"{companyTitle} görüşmesine not eklendi.",
            Domain.Enums.NotificationType.MeetingNoteAdded,
            title: "Görüşme Notu",
            link: $"/companies/{meeting.CompanyId}",
            senderUserId: currentUser.UserId,
            senderName: null,
            cancellationToken);
    }

    private async Task<(string AuthorName, string? AuthorTitle)> ResolveAuthorAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return ("Sistem", null);

        var userInfo = await identityService.GetByIdAsync(userId, cancellationToken);
        if (userInfo is null)
            return ("Bilinmeyen Kullanıcı", null);

        var authorName = userInfo.FullName;
        var authorTitle = userInfo.Position;

        if (string.IsNullOrWhiteSpace(authorTitle))
        {
            var employeeList = await employees.ListAsync(
                e => e.ApplicationUserId == userId,
                cancellationToken);

            authorTitle = employeeList.FirstOrDefault()?.Title;
        }

        return (authorName, authorTitle);
    }

    private static MailDraft BuildReminderDraft(Meeting meeting, Company company, SalesRep rep, Contact? contact)
    {
        var subject = $"{company.Title} ile Yaklaşan Etkinlik!";
        var body =
            $"Sayın {rep.Name},\n\n" +
            $"{company.Title} ile yaklaşan etkinlik!\n\n" +
            "Detaylar:\n" +
            $"Firma Temsilcisi: {contact?.Name ?? "Belirtilmedi"}\n" +
            $"Tarih: {meeting.Date:yyyy-MM-dd}\n" +
            $"Saat: {meeting.Time:HH\\:mm}\n" +
            $"Adres: {meeting.Address}\n" +
            $"Yöntem: {MethodLabel(meeting.Method)}";

        return new MailDraft(rep.Email, subject, body, meeting.Id, contact?.Email);
    }

    public async Task<PagedResult<MeetingDto>> GetPagedAsync(
        PagedQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await meetings.ListPagedAsync(
            query.Search,
            query.SortBy,
            query.IsDescending,
            query.Page,
            query.PageSize,
            cancellationToken);

        var dtos = items.Select(m => m.ToDto()).ToList();
        return new PagedResult<MeetingDto>(dtos, query.Page, query.PageSize, totalCount);
    }

    private static string MethodLabel(MeetingMethod method) => method switch
    {
        MeetingMethod.Visit => "Yüz Yüze Ziyaret",
        MeetingMethod.Phone => "Telefon Görüşmesi",
        MeetingMethod.Email => "E-mail / Teklif",
        _ => method.ToString()
    };
}
