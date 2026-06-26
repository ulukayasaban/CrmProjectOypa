using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Mappings;

public static class CompanyNoteMappings
{
    public static CompanyNoteDto ToDto(this CompanyNote n) => new(
        n.Id,
        n.Content,
        n.AuthorName,
        n.CreatedAtUtc);
}
