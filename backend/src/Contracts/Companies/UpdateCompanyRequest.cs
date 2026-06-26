using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Companies;

public sealed record UpdateCompanyRequest(
    string Title,
    Sector Sector,
    string Phone,
    string Email,
    string Address,
    string? City = null,
    string? Website = null,
    string? TaxNumber = null,
    CompanySource? Source = null,
    ServiceSector? ServiceSector = null,
    FirmType FirmType = FirmType.DisFirma,
    string? SourceNote = null,
    Guid? LeadOwnerId = null);
