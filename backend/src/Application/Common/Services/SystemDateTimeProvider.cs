using Oypa.Crm.Application.Common.Interfaces;

namespace Oypa.Crm.Application.Common.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
