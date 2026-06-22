using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Companies;

public sealed record SetCustomerStatusRequest(CustomerStatus Status);
