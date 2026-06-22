using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Tenders;

public sealed record UpdateTenderRequest(
    Guid CompanyId,
    string Title,
    string? TenderNumber,
    Sector Sector,
    DateOnly TenderDate,
    int? PersonnelCount,
    decimal? EstimatedValue,
    decimal? Volume,
    int? Quantity,
    string? Description,
    Guid? AssignedSalesRepId);
