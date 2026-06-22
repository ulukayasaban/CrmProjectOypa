using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Entities;

/// <summary>Sistem tarafından üretilen otomatik hatırlatma maili taslağı (simülasyon).</summary>
public class MailDraft : BaseEntity
{
    private MailDraft() { }

    public MailDraft(string to, string subject, string body, Guid? meetingId = null, string? cc = null)
    {
        To = to;
        Subject = subject;
        Body = body;
        MeetingId = meetingId;
        Cc = cc;
    }

    public string To { get; private set; } = string.Empty;
    public string? Cc { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool Sent { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public Guid? MeetingId { get; private set; }

    public void MarkSent()
    {
        if (Sent) return;
        Sent = true;
        SentAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
