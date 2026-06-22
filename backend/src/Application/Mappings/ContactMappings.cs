using Oypa.Crm.Contracts.Contacts;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Mappings;

public static class ContactMappings
{
    public static ContactDto ToDto(this Contact c) => new(
        c.Id,
        c.CompanyId,
        c.Name,
        c.Email,
        c.Phone);
}
