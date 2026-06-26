namespace Oypa.Crm.Application.Features.Companies;

/// <summary>
/// Müşteri aktivite durumu yönetim servisi.
/// Test edilebilirlik için Application katmanında tanımlanır;
/// zamanlama mantığı <c>Api/BackgroundServices/CustomerActivityStatusHostedService</c>'te bulunur.
/// </summary>
public interface ICustomerActivityService
{
    /// <summary>
    /// Son etkileşim tarihi 6 aydan eski olan aktif müşterileri pasife düşürür.
    /// Son etkileşim tarihi null ise <c>ActivatedAtUtc</c>, o da null ise <c>CreatedAtUtc</c> baz alınır.
    /// </summary>
    /// <returns>Pasife alınan müşteri sayısı.</returns>
    Task<int> DeactivateInactiveCustomersAsync(CancellationToken cancellationToken = default);
}
