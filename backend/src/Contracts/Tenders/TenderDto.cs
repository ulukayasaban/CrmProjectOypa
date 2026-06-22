namespace Oypa.Crm.Contracts.Tenders;

public sealed record TenderDto(
    Guid Id,
    Guid CompanyId,
    string CompanyTitle,
    string Title,
    string? TenderNumber,
    string Sector,
    DateOnly TenderDate,
    string Status,
    int? PersonnelCount,
    decimal? EstimatedValue,
    decimal? Volume,
    int? Quantity,
    string? Description,
    Guid? AssignedSalesRepId,
    string? AssignedSalesRepName,
    DateTime CreatedAtUtc);
