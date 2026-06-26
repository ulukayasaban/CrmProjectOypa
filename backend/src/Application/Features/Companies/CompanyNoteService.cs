using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Mappings;
using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Features.Companies;

public sealed class CompanyNoteService(
    IRepository<Company> companies,
    IRepository<CompanyNote> companyNotes,
    ICurrentUser currentUser,
    IIdentityService identityService,
    IDateTimeProvider clock,
    IUnitOfWork unitOfWork) : ICompanyNoteService
{
    public async Task<IReadOnlyList<CompanyNoteDto>> GetByCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var notes = await companyNotes.ListAsync(
            n => n.CompanyId == companyId,
            cancellationToken);

        return notes
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => n.ToDto())
            .ToList()
            .AsReadOnly();
    }

    public async Task<CompanyNoteDto> AddAsync(
        Guid companyId,
        CreateCompanyNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var company = await companies.GetByIdAsync(companyId, cancellationToken)
            ?? throw NotFoundException.For("Firma", companyId);

        var authorName = await ResolveAuthorNameAsync(cancellationToken);

        var note = new CompanyNote(companyId, request.Content, currentUser.UserId, authorName);
        await companyNotes.AddAsync(note, cancellationToken);

        // Not eklendiğinde firmada etkileşim kaydı güncellenir (pasif müşteriyse aktife döner).
        company.RegisterInteraction(clock.UtcNow, reactivate: true);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return note.ToDto();
    }

    private async Task<string> ResolveAuthorNameAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return "Sistem";

        var userInfo = await identityService.GetByIdAsync(userId, cancellationToken);
        if (userInfo is null)
            return "Bilinmeyen Kullanıcı";

        return string.IsNullOrWhiteSpace(userInfo.FullName)
            ? (currentUser.Email ?? "Bilinmeyen Kullanıcı")
            : userInfo.FullName;
    }
}
