using Oypa.Crm.Contracts.Common;
using Oypa.Crm.Contracts.Meetings;

namespace Oypa.Crm.Application.Features.Meetings;

public interface IMeetingService
{
    Task<IReadOnlyList<MeetingDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeetingDto>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<MeetingDto> ScheduleAsync(ScheduleMeetingRequest request, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(Guid id, UpdateMeetingStatusRequest request, CancellationToken cancellationToken = default);

    /// <summary>Belirtilen görüşmeye not ekler; güncel <see cref="MeetingDto"/> döndürür (notlar dahil).</summary>
    Task<MeetingDto> AddNoteAsync(Guid meetingId, AddMeetingNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Görüşmeleri sayfalama + arama + sıralama ile listeler.</summary>
    Task<PagedResult<MeetingDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken = default);
}
