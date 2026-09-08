using projectManager2Core.DTOs;

namespace projectManager2Core.Services;

public interface IEventService
{
    Task<EventDto> CreateAsync(int currentUserId, CreateEventDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// גישה מוגבלת ל-Creator או EventMember בלבד - לכל המשתמשים, ללא יוצא
    /// מן הכלל (אין Role שרואה הכל).
    /// </summary>
    Task<EventDto> GetByIdAsync(int currentUserId, int eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// מדופדף (Pagination) - ראו PagedResultDto. pageNumber/pageSize מכווצים
    /// לטווח הגיוני ב-EventService (ראו PaginationHelper).
    /// </summary>
    Task<PagedResultDto<EventDto>> GetAllAsync(int currentUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<EventDto> UpdateAsync(int currentUserId, int eventId, UpdateEventDto dto, CancellationToken cancellationToken = default);

    Task<EventDto> CompleteAsync(int currentUserId, int eventId, CancellationToken cancellationToken = default);
}
