using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Contracts.Tenders;

public sealed record ChangeTenderStatusRequest(TenderStatus Status);
