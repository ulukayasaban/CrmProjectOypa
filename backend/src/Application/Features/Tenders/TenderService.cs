using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Mappings;
using Oypa.Crm.Contracts.Tenders;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Tenders;

public sealed class TenderService(
    ITenderRepository tenders,
    IRepository<Company> companies,
    IRepository<SalesRep> salesReps,
    IUnitOfWork unitOfWork) : ITenderService
{
    public async Task<IReadOnlyList<TenderDto>> GetAsync(
        Sector? sector,
        TenderStatus? status,
        CancellationToken cancellationToken = default)
    {
        var list = await tenders.ListAsync(sector, status, cancellationToken);
        return list.Select(t => t.ToDto()).ToList();
    }

    public async Task<TenderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tender = await tenders.GetByIdWithDetailsAsync(id, cancellationToken)
                     ?? throw NotFoundException.For("İhale", id);

        return tender.ToDto();
    }

    public async Task<TenderDto> CreateAsync(CreateTenderRequest request, CancellationToken cancellationToken = default)
    {
        _ = await companies.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw NotFoundException.For("Firma", request.CompanyId);

        if (request.AssignedSalesRepId is { } repId)
        {
            _ = await salesReps.GetByIdAsync(repId, cancellationToken)
                ?? throw NotFoundException.For("Satış temsilcisi", repId);
        }

        var tender = Tender.Create(
            request.CompanyId,
            request.Title,
            request.TenderNumber,
            request.Sector,
            request.TenderDate,
            request.PersonnelCount,
            request.EstimatedValue,
            request.Volume,
            request.Quantity,
            request.Description,
            request.AssignedSalesRepId);

        await tenders.AddAsync(tender, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Detay navigation'ları ile yeniden çek
        var created = await tenders.GetByIdWithDetailsAsync(tender.Id, cancellationToken)
                      ?? throw NotFoundException.For("İhale", tender.Id);

        return created.ToDto();
    }

    public async Task<TenderDto> UpdateAsync(Guid id, UpdateTenderRequest request, CancellationToken cancellationToken = default)
    {
        var tender = await tenders.GetByIdAsync(id, cancellationToken)
                     ?? throw NotFoundException.For("İhale", id);

        _ = await companies.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw NotFoundException.For("Firma", request.CompanyId);

        if (request.AssignedSalesRepId is { } repId)
        {
            _ = await salesReps.GetByIdAsync(repId, cancellationToken)
                ?? throw NotFoundException.For("Satış temsilcisi", repId);
        }

        tender.UpdateDetails(
            request.Title,
            request.TenderNumber,
            request.Sector,
            request.TenderDate,
            request.PersonnelCount,
            request.EstimatedValue,
            request.Volume,
            request.Quantity,
            request.Description,
            request.AssignedSalesRepId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await tenders.GetByIdWithDetailsAsync(id, cancellationToken)
                      ?? throw NotFoundException.For("İhale", id);

        return updated.ToDto();
    }

    public async Task ChangeStatusAsync(Guid id, ChangeTenderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var tender = await tenders.GetByIdAsync(id, cancellationToken)
                     ?? throw NotFoundException.For("İhale", id);

        tender.ChangeStatus(request.Status);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tender = await tenders.GetByIdAsync(id, cancellationToken)
                     ?? throw NotFoundException.For("İhale", id);

        tenders.Remove(tender);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
