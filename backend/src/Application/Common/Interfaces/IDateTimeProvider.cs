namespace Oypa.Crm.Application.Common.Interfaces;

/// <summary>Test edilebilirlik için zaman soyutlaması.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
