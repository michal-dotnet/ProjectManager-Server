using projectManager2Core.Models;

namespace projectManager2Core.Repositories;

public interface IUserRepository
{
    /// <summary>
    /// קריאה-בלבד בכל המערכת (אין endpoint לעדכון User) - ולכן משתמשת
    /// ב-AsNoTracking בממימוש.
    /// </summary>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// מדופדף - מחזיר גם את הסה"כ (TotalCount) לצורך חישוב TotalPages.
    /// </summary>
    Task<(List<User> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task AddAsync(User user, CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
