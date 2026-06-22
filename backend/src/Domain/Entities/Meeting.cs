using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Enums;
using Oypa.Crm.Domain.Events;

namespace Oypa.Crm.Domain.Entities;

/// <summary>Bir firma ile planlanan randevu/görüşme.</summary>
public class Meeting : BaseEntity
{
    private Meeting() { }

    private Meeting(
        Guid companyId,
        Guid salesRepId,
        Guid? contactId,
        DateOnly date,
        TimeOnly time,
        string address,
        MeetingMethod method)
    {
        CompanyId = companyId;
        SalesRepId = salesRepId;
        ContactId = contactId;
        Date = date;
        Time = time;
        Address = address;
        Method = method;
        Status = MeetingStatus.Planned;
    }

    public Guid CompanyId { get; private set; }
    public Company? Company { get; private set; }

    public Guid SalesRepId { get; private set; }
    public SalesRep? SalesRep { get; private set; }

    public Guid? ContactId { get; private set; }
    public Contact? Contact { get; private set; }

    public DateOnly Date { get; private set; }
    public TimeOnly Time { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public MeetingMethod Method { get; private set; }
    public MeetingStatus Status { get; private set; }
    public string? Comment { get; private set; }

    private readonly List<MeetingNote> _notes = [];

    /// <summary>Görüşmeye eklenen notlar (salt-okunur koleksiyon).</summary>
    public IReadOnlyCollection<MeetingNote> Notes => _notes.AsReadOnly();

    /// <summary>Yeni görüşme oluşturur ve <see cref="MeetingScheduledEvent"/> tetikler.</summary>
    public static Meeting Schedule(
        Guid companyId,
        Guid salesRepId,
        Guid? contactId,
        DateOnly date,
        TimeOnly time,
        string address,
        MeetingMethod method)
    {
        var meeting = new Meeting(companyId, salesRepId, contactId, date, time, address, method);
        meeting.RaiseDomainEvent(new MeetingScheduledEvent(meeting.Id, companyId, salesRepId, contactId));
        return meeting;
    }

    public void MarkAsDone(string? comment)
    {
        Status = MeetingStatus.Done;
        Comment = comment;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string? comment)
    {
        Status = MeetingStatus.Cancelled;
        Comment = comment;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Görüşmeye yeni not ekler; boş içerik reddedilir.</summary>
    public void AddNote(string content, Guid? authorUserId, string authorName, string? authorTitle)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Not içeriği boş olamaz.", nameof(content));

        _notes.Add(new MeetingNote(Id, content, authorUserId, authorName, authorTitle));
    }
}
