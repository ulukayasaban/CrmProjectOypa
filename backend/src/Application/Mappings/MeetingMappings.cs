using Oypa.Crm.Contracts.Meetings;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Mappings;

public static class MeetingMappings
{
    /// <summary>
    /// İlişkili Company/SalesRep/Contact ve SalesRep.Employee navigation'larının,
    /// ayrıca Notes koleksiyonunun yüklü olduğunu varsayar.
    /// </summary>
    public static MeetingDto ToDto(this Meeting m) => new(
        m.Id,
        m.CompanyId,
        m.Company?.Title ?? string.Empty,
        m.ContactId,
        m.Contact?.Name,
        m.SalesRepId,
        m.SalesRep?.Name ?? string.Empty,
        m.SalesRep?.Employee?.Title,
        m.Date,
        m.Time,
        m.Address,
        m.Method,
        m.Status,
        m.Comment,
        m.Notes
            .OrderBy(n => n.CreatedAtUtc)
            .Select(n => n.ToDto())
            .ToList()
            .AsReadOnly());

    public static MeetingNoteDto ToDto(this MeetingNote n) => new(
        n.Id,
        n.Content,
        n.AuthorName,
        n.AuthorTitle,
        n.CreatedAtUtc);
}
