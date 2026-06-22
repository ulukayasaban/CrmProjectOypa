using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Companies;

public sealed record SetLeadStatusRequest(LeadStatus Status);
