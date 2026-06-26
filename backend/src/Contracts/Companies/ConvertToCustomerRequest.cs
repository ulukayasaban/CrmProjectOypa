using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Companies;

/// <summary>
/// Lead'i müşteriye dönüştürme isteği.
/// Tüm alanlar opsiyoneldir; mevcut convert davranışı geriye dönük korunur.
/// </summary>
public sealed record ConvertToCustomerRequest(
    /// <summary>
    /// Dönüşüm sırasında atanacak satış temsilcisi kimliği.
    /// Null ise firma havuza alınır.
    /// </summary>
    Guid? SalesRepId = null,

    /// <summary>
    /// OYPA'nın bu firmaya sunduğu hizmet sektörü.
    /// Null ise mevcut değer korunur.
    /// </summary>
    ServiceSector? ServiceSector = null,

    /// <summary>Firma yeni müşteri mi? Varsayılan false.</summary>
    bool IsNewCustomer = false);
