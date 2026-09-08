using projectManager2Core.Models;

namespace projectManager2Core.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// טוען Event יחד עם Members ו-Tasks (לצורך בדיקות הרשאה/עסק שדורשות אותם).
    /// קריאה-בלבד (אף פעם לא ממשיכה ל-SaveChangesAsync על אותו Event) -
    /// ולכן משתמשת ב-AsNoTracking בממימוש.
    /// </summary>
    Task<Event?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// כל ה-Events שבהם המשתמש הוא Creator או Member - זו רשימת ה-Events
    /// היחידה שקיימת במערכת (אין Role שרואה את כל ה-Events ללא סינון).
    /// מדופדף - מחזיר גם את הסה"כ (TotalCount) לצורך חישוב TotalPages.
    /// </summary>
    Task<(List<Event> Items, int TotalCount)> GetAllForUserAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task AddAsync(Event @event, CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
