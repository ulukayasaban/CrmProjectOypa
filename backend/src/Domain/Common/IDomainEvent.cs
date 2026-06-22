namespace Oypa.Crm.Domain.Common;

/// <summary>Bir domain içinde gerçekleşmiş, yan etkileri tetikleyebilen olayı işaretler.</summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
