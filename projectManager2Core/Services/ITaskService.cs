using projectManager2Core.DTOs;

namespace projectManager2Core.Services;

public interface ITaskService
{
    Task<TaskDto> CreateAsync(int currentUserId, int eventId, CreateTaskDto dto, CancellationToken cancellationToken = default);

    Task<TaskDto> GetByIdAsync(int currentUserId, int eventId, int taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// מדופדף (Pagination) - ראו PagedResultDto. pageNumber/pageSize מכווצים
    /// לטווח הגיוני ב-TaskService (ראו PaginationHelper).
    /// </summary>
    Task<PagedResultDto<TaskDto>> GetByEventIdAsync(int currentUserId, int eventId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<TaskDto> UpdateAsync(int currentUserId, int eventId, int taskId, UpdateTaskDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// הפעולה העסקית הייעודית היחידה שמאפשרת הקצאת Task למשתמש הנוכחי.
    /// אטומית ברמת ה-Database; במקרה שהמשימה כבר נלקחה על ידי מישהו אחר
    /// (Race) תיזרק ConflictException שתמופה ל-409.
    /// </summary>
    Task<TaskDto> TakeAsync(int currentUserId, int eventId, int taskId, CancellationToken cancellationToken = default);
}
