using Oypa.Crm.Contracts.Tenders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Mappings;

public static class TenderMappings
{
    /// <summary>
    /// Company ve AssignedSalesRep navigation'larının yüklü olduğunu varsayar.
    /// </summary>
    public static TenderDto ToDto(this Tender t) => new(
        t.Id,
        t.CompanyId,
        t.Company?.Title ?? string.Empty,
        t.Title,
        t.TenderNumber,
        t.Sector.ToString(),
        t.TenderDate,
        t.Status.ToString(),
        t.PersonnelCount,
        t.EstimatedValue,
        t.Volume,
        t.Quantity,
        t.Description,
        t.AssignedSalesRepId,
        t.AssignedSalesRep?.Name,
        t.CreatedAtUtc);
}
