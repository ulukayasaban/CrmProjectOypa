using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Mappings;
using Oypa.Crm.Contracts.Companies;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Application.Features.Companies;

public sealed class CompanyService(
    IRepository<Company> companies,
    ICompanyRepository companyRepository,
    IRepository<SalesRep> salesReps,
    IUnitOfWork unitOfWork) : ICompanyService
{
    public async Task<IReadOnlyList<CompanyDto>> GetLeadsAsync(LeadStatus? status = null, CancellationToken cancellationToken = default)
    {
        var list = await companyRepository.ListLeadsAsync(status, cancellationToken);
        return list.Select(c => c.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<CompanyDto>> GetCustomersAsync(CustomerStatus? status = null, CancellationToken cancellationToken = default)
    {
        var list = await companyRepository.ListCustomersAsync(status, cancellationToken);
        return list.Select(c => c.ToDto()).ToList();
    }

    public async Task<CompanyDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetByIdWithRepAsync(id, cancellationToken)
                      ?? throw NotFoundException.For("Firma", id);
        return company.ToDto();
    }

    public async Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = new Company(
            request.Title,
            request.Sector,
            request.Phone,
            request.Email,
            request.Address,
            request.City,
            request.Website,
            request.TaxNumber,
            request.Source);
        await companies.AddAsync(company, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return company.ToDto();
    }

    public async Task<CompanyDto> UpdateAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = await companies.GetByIdAsync(id, cancellationToken)
                      ?? throw NotFoundException.For("Firma", id);

        company.UpdateDetails(
            request.Title,
            request.Sector,
            request.Phone,
            request.Email,
            request.Address,
            request.City,
            request.Website,
            request.TaxNumber,
            request.Source);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return company.ToDto();
    }

    public async Task SetLeadStatusAsync(Guid id, LeadStatus status, CancellationToken cancellationToken = default)
    {
        var company = await companies.GetByIdAsync(id, cancellationToken)
                      ?? throw NotFoundException.For("Firma", id);

        if (company.Type != CompanyType.Lead)
            throw new ConflictException("Yalnızca lead aşamasındaki firmaların durumu değiştirilebilir.");

        company.SetLeadStatus(status);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetCustomerStatusAsync(Guid id, CustomerStatus status, CancellationToken cancellationToken = default)
    {
        var company = await companies.GetByIdAsync(id, cancellationToken)
                      ?? throw NotFoundException.For("Firma", id);

        if (company.Type != CompanyType.Customer)
            throw new ConflictException("Müşteri durumu yalnızca müşteri aşamasındaki firmalarda değiştirilebilir.");

        company.SetCustomerStatus(status);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CompanyDto> ConvertToCustomerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await companies.GetByIdAsync(id, cancellationToken)
                      ?? throw NotFoundException.For("Firma", id);

        if (company.Type == CompanyType.Customer)
            throw new ConflictException("Firma zaten müşteri.");

        // ConvertToCustomer() LeadConvertedToCustomerEvent tetikler;
        // event handler yönetici zincirine otomatik bildirim üretir.
        company.ConvertToCustomer();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return company.ToDto();
    }

    public async Task AssignSalesRepAsync(Guid id, Guid? salesRepId, CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetByIdWithRepAsync(id, cancellationToken)
                      ?? throw NotFoundException.For("Firma", id);

        if (salesRepId.HasValue)
        {
            _ = await salesReps.GetByIdAsync(salesRepId.Value, cancellationToken)
                ?? throw NotFoundException.For("Satış temsilcisi", salesRepId.Value);
        }

        company.AssignSalesRep(salesRepId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
