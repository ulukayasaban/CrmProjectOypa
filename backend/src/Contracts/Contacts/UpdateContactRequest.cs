namespace Oypa.Crm.Contracts.Contacts;

/// <summary>İlgili kişi güncelleme isteği. Alanlar mevcut CreateContactRequest ile aynı yapıda tutulur.</summary>
public sealed record UpdateContactRequest(
    string Name,
    string? Email,
    string? Phone);
