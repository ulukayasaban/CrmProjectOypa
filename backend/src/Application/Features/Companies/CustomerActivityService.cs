using Microsoft.Extensions.Logging;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Companies;

/// <summary>
/// Son etkileşim tarihi 6 aydan eski aktif müşterileri pasife düşürür.
/// Zamanlayıcı mantığı <c>CustomerActivityStatusHostedService</c>'te tutulur;
/// bu sınıf yalnızca iş kuralını uygular ve test edilebilir.
/// </summary>
public sealed class CustomerActivityService(
    ICompanyRepository companyRepository,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<CustomerActivityService> logger) : ICustomerActivityService
{
    private static readonly TimeSpan InactivityThreshold = TimeSpan.FromDays(180); // 6 ay

    public async Task<int> DeactivateInactiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = clock.UtcNow - InactivityThreshold;

        // Tüm aktif müşterileri çek (tracking açık — SetCustomerStatus çağrılacak)
        var activeCustomers = await companyRepository.ListCustomersAsync(
            CustomerStatus.Active,
            cancellationToken);

        var toDeactivate = activeCustomers
            .Where(c => GetLastActivityDate(c) < cutoff)
            .ToList();

        if (toDeactivate.Count == 0)
        {
            logger.LogInformation("CustomerActivityService: Pasife alınacak müşteri bulunamadı.");
            return 0;
        }

        foreach (var company in toDeactivate)
        {
            company.SetCustomerStatus(CustomerStatus.Passive);
            logger.LogInformation(
                "CustomerActivityService: Müşteri {CompanyId} ({Title}) pasife alındı. Son etkileşim: {LastActivity:O}.",
                company.Id,
                company.Title,
                GetLastActivityDate(company));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "CustomerActivityService: {Count} müşteri pasife alındı.",
            toDeactivate.Count);

        return toDeactivate.Count;
    }

    /// <summary>
    /// Firmanın baz alınacak tarihini döndürür:
    /// LastInteractionAtUtc → ActivatedAtUtc → CreatedAtUtc sıralamasıyla.
    /// </summary>
    private static DateTime GetLastActivityDate(Company company) =>
        company.LastInteractionAtUtc
        ?? company.ActivatedAtUtc
        ?? company.CreatedAtUtc;
}
