using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>Company'ye özgü sorgular: ilişkili temsilci include edilir.</summary>
public interface ICompanyRepository : IRepository<Company>
{
    /// <summary>Lead firmaları opsiyonel durum filtresine göre, temsilciyle birlikte getirir.</summary>
    Task<IReadOnlyList<Company>> ListLeadsAsync(LeadStatus? status, CancellationToken cancellationToken = default);

    /// <summary>Müşteri firmaları opsiyonel durum filtresine göre, temsilciyle birlikte getirir.</summary>
    Task<IReadOnlyList<Company>> ListCustomersAsync(CustomerStatus? status, CancellationToken cancellationToken = default);

    /// <summary>Firmayı atanan temsilciyle birlikte getirir (tracking açık — güncelleme için).</summary>
    Task<Company?> GetByIdWithRepAsync(Guid id, CancellationToken cancellationToken = default);
}
