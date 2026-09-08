namespace projectManager2Core.Repositories;

public interface ITaskRepository
{
    Task<TaskEntity?> GetByIdAsync(int eventId, int taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// מדופדף - מחזיר גם את הסה"כ (TotalCount) לצורך חישוב TotalPages.
    /// </summary>
    Task<(List<TaskEntity> Items, int TotalCount)> GetByEventIdAsync(int eventId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task AddAsync(TaskEntity task, CancellationToken cancellationToken = default);

    /// <summary>
    /// שומר שינויים על Task שנטענה ונערכה על ה-Context (Update רגיל, לרבות
    /// TakeAsync - ראו TaskService). ההגנה מפני שני משתמשים שמעדכנים את
    /// אותה Task בו-זמנית (למשל שניהם "לוקחים" אותה כמעט באותו רגע) היא
    /// Concurrency Token אמיתי (TaskEntity.ConcurrencyVersion) - אם השורה
    /// כבר השתנתה בינתיים, נזרקת כאן ConflictException (ולא
    /// DbUpdateConcurrencyException של EF Core - ה-Service layer לא צריך
    /// לדעת שום דבר על EF Core).
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
