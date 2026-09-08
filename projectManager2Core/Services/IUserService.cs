using projectManager2Core.DTOs;

namespace projectManager2Core.Services;

/// <summary>
/// יצירת משתמשים עוברת אך ורק דרך IAuthService.RegisterAsync (עם סיסמה,
/// Hash ובחירת Role עצמית) - אין כאן CreateAsync, כדי שלא תהיה דרך ליצור
/// משתמש בלי סיסמה או עם Role שרירותי.
/// </summary>
public interface IUserService
{
    Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// מדופדף (Pagination) - ראו PagedResultDto. pageNumber/pageSize מכווצים
    /// לטווח הגיוני ב-UserService (ראו PaginationHelper).
    /// </summary>
    Task<PagedResultDto<UserDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
