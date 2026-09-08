using projectManager2Core.DTOs;

namespace projectManager2Core.Services;

public interface IEventMemberService
{
    Task<EventMemberDto> AddAsync(int currentUserId, int eventId, AddEventMemberDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// מדופדף (Pagination) - ראו PagedResultDto. pageNumber/pageSize מכווצים
    /// לטווח הגיוני ב-EventMemberService (ראו PaginationHelper).
    /// </summary>
    Task<PagedResultDto<EventMemberDto>> GetByEventIdAsync(int currentUserId, int eventId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task RemoveAsync(int currentUserId, int eventId, int userId, CancellationToken cancellationToken = default);
}
