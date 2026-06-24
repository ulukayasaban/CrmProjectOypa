using Oypa.Crm.Contracts.Contacts;

namespace Oypa.Crm.Application.Features.Contacts;

public interface IContactService
{
    Task<IReadOnlyList<ContactDto>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<ContactDto> AddAsync(Guid companyId, CreateContactRequest request, CancellationToken cancellationToken = default);

    /// <summary>Belirtilen ilgili kişiyi Id ile döndürür; bulunamazsa NotFoundException fırlatır.</summary>
    Task<ContactDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>İlgili kişiyi günceller; bulunamazsa NotFoundException fırlatır.</summary>
    Task<ContactDto> UpdateAsync(Guid id, UpdateContactRequest request, CancellationToken cancellationToken = default);

    /// <summary>İlgili kişiyi siler; bulunamazsa NotFoundException fırlatır.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
