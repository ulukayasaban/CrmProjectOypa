using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Mappings;
using Oypa.Crm.Contracts.Contacts;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Features.Contacts;

public sealed class ContactService(
    IRepository<Company> companies,
    IRepository<Contact> contacts,
    IUnitOfWork unitOfWork) : IContactService
{
    public async Task<IReadOnlyList<ContactDto>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = await contacts.ListAsync(c => c.CompanyId == companyId, cancellationToken);
        return list.Select(c => c.ToDto()).ToList();
    }

    public async Task<ContactDto> AddAsync(Guid companyId, CreateContactRequest request, CancellationToken cancellationToken = default)
    {
        if (await companies.GetByIdAsync(companyId, cancellationToken) is null)
            throw NotFoundException.For("Firma", companyId);

        var contact = new Contact(companyId, request.Name, request.Email, request.Phone);
        await contacts.AddAsync(contact, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return contact.ToDto();
    }
}
