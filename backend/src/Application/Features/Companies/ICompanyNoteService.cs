using Oypa.Crm.Contracts.Companies;

namespace Oypa.Crm.Application.Features.Companies;

public interface ICompanyNoteService
{
    /// <summary>Firmaya ait notları oluşturulma zamanına göre yeni→eski sıralar.</summary>
    Task<IReadOnlyList<CompanyNoteDto>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Firmaya yeni not ekler; yazarı ICurrentUser'dan alır.</summary>
    Task<CompanyNoteDto> AddAsync(Guid companyId, CreateCompanyNoteRequest request, CancellationToken cancellationToken = default);
}
