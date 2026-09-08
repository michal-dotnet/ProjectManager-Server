using System.Security.Claims;
using projectManager2.Middlewares;
using projectManager2Core.Enums;
using projectManager2Core.Services;

namespace projectManager2.Helpers;

/// <summary>
/// מימוש ICurrentUserService התלוי ב-HttpContext, ולכן חי בשכבת ה-Api
/// (לא ב-Core, כדי לא להכניס תלות ב-ASP.NET Core לשכבה עסקית). הערכים
/// נשלפים מ-HttpContext.User - ה-ClaimsPrincipal שה-JWT Bearer Middleware
/// (ראו Program.cs) ממלא אוטומטית לאחר אימות Token מוצלח. אין יותר תלות
/// ב-CurrentUserMiddleware הישן (X-User-Id Header) - הקובץ הזה כבר לא
/// רשום ב-Program.cs ואפשר למחוק אותו ידנית מה-Solution.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (value is not null && int.TryParse(value, out var userId))
            {
                return userId;
            }

            throw new InvalidOperationException(
                "לא נמצא Claim מסוג NameIdentifier ב-HttpContext.User. ודאו שהבקשה עברה אימות JWT ([Authorize]) בהצלחה.");
        }
    }

    public UserRole Role
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(value, out var role) ? role : UserRole.Worker;
        }
    }
}
