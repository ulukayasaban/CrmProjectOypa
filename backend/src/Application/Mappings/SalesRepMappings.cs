using Oypa.Crm.Contracts.SalesReps;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Application.Mappings;

public static class SalesRepMappings
{
    public static SalesRepDto ToDto(this SalesRep r) => new(r.Id, r.Name, r.Email, r.EmployeeId);
}
