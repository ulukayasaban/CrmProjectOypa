using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Mappings;

public static class CompanyMappings
{
    public static CompanyDto ToDto(this Company c) => new(
        c.Id,
        c.Title,
        c.Sector,
        c.Phone,
        c.Email,
        c.Address,
        c.City,
        c.Website,
        c.TaxNumber,
        c.Source,
        c.Type,
        c.LeadStatus,
        c.CustomerStatus,
        c.ActivatedAtUtc,
        c.CreatedAtUtc,
        c.AssignedSalesRepId,
        c.AssignedSalesRep?.Name,
        c.Categories.Select(cat => cat.ToDto()).ToList().AsReadOnly(),
        c.ServiceSector,
        c.FirmType,
        c.SourceNote,
        c.LeadOwnerId,
        c.LeadOwner?.Name);
}
