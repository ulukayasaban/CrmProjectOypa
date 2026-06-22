using Oypa.Crm.Application.Common.Exceptions;
using Oypa.Crm.Application.Common.Interfaces;
using Oypa.Crm.Application.Mappings;
using Oypa.Crm.Contracts.SalesReps;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Features.SalesReps;

public sealed class SalesRepService(
    IRepository<SalesRep> salesReps,
    IRepository<Employee> employees,
    IUnitOfWork unitOfWork) : ISalesRepService
{
    public async Task<IReadOnlyList<SalesRepDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await salesReps.ListAsync(cancellationToken);
        return list.Select(r => r.ToDto()).ToList();
    }

    public async Task<SalesRepDto> CreateAsync(CreateSalesRepRequest request, CancellationToken cancellationToken = default)
    {
        var rep = new SalesRep(request.Name, request.Email);
        await salesReps.AddAsync(rep, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return rep.ToDto();
    }

    public async Task<SalesRepDto> LinkEmployeeAsync(Guid id, Guid? employeeId, CancellationToken cancellationToken = default)
    {
        var rep = await salesReps.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Satış temsilcisi", id);

        if (employeeId.HasValue)
        {
            var employeeExists = await employees.GetByIdAsync(employeeId.Value, cancellationToken);
            if (employeeExists is null)
                throw NotFoundException.For("Personel", employeeId.Value);
        }

        rep.LinkEmployee(employeeId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return rep.ToDto();
    }
}
