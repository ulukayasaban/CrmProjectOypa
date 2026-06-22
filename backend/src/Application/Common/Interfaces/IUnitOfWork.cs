namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>Bir iş işlemindeki tüm değişiklikleri atomik olarak kalıcılaştırır.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
