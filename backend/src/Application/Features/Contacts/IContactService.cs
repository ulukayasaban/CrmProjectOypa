using Oypa.Crm.Contracts.Contacts;

namespace Oypa.Crm.Application.Features.Contacts;

public interface IContactService
{
    Task<IReadOnlyList<ContactDto>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<ContactDto> AddAsync(Guid companyId, CreateContactRequest request, CancellationToken cancellationToken = default);
}
