namespace Oypa.Crm.Contracts.Dashboard;

/// <summary>Haftanın bir günü için planlanan görüşme sayısı.</summary>
public sealed record WeeklyDensityPoint(string Day, int Count);
