namespace Oypa.Crm.Contracts.Companies;

/// <summary>
/// Firmaya satış temsilcisi atama isteği.
/// <see cref="SalesRepId"/> null gönderilirse firma havuza alınır.
/// </summary>
public sealed record AssignSalesRepRequest(Guid? SalesRepId);
