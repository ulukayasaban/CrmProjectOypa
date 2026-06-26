using Oypa.Crm.Contracts.Categories;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Companies;

public sealed record CompanyDto(
    Guid Id,
    string Title,
    Sector Sector,
    string Phone,
    string Email,
    string Address,
    string? City,
    string? Website,
    string? TaxNumber,
    CompanySource? Source,
    CompanyType Type,
    LeadStatus? LeadStatus,
    CustomerStatus? CustomerStatus,
    DateTime? ActivatedAtUtc,
    DateTime CreatedAtUtc,
    Guid? AssignedSalesRepId,
    string? AssignedSalesRepName,
    IReadOnlyList<CategoryDto> Categories,
    ServiceSector? ServiceSector,
    FirmType FirmType,
    string? SourceNote,
    Guid? LeadOwnerId,
    string? LeadOwnerName,
    bool IsNewCustomer,
    DateTime? LastInteractionAtUtc);
