using Oypa.Crm.Contracts.Goals;

namespace Oypa.Crm.Application.Features.Goals;

public interface IGoalService
{
    /// <summary>Çağıranın kapsamındaki aktif hedefleri ve içinde bulunulan hafta ilerlemesini döndürür.</summary>
    Task<IReadOnlyList<GoalDto>> GetScopedAsync(CancellationToken cancellationToken = default);

    Task<GoalDto> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken = default);

    Task<GoalDto> UpdateAsync(Guid id, UpdateGoalRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Belirtilen hedefe ait haftalık snapshot geçmişini döndürür.</summary>
    Task<IReadOnlyList<GoalWeekDto>> GetWeeksAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Dashboard için: kapsamdaki hedeflerin bu haftaki ilerleme özetlerini döndürür.</summary>
    Task<IReadOnlyList<GoalProgressDto>> GetScopedProgressAsync(CancellationToken cancellationToken = default);
}
