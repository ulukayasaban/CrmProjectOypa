using Oypa.Crm.Contracts.MailDrafts;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Mappings;

public static class MailDraftMappings
{
    public static MailDraftDto ToDto(this MailDraft d) => new(
        d.Id,
        d.To,
        d.Cc,
        d.Subject,
        d.Body,
        d.Sent,
        d.SentAtUtc,
        d.CreatedAtUtc,
        d.MeetingId);
}
