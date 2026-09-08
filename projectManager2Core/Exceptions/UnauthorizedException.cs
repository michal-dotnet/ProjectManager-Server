namespace projectManager2Core.Exceptions;

/// <summary>
/// פרטי התחברות שגויים (ממופה ל-401 Unauthorized). שונה מ-ForbiddenException:
/// כאן הזהות עצמה לא אומתה בהצלחה, לעומת Forbidden שבו הזהות ידועה אבל
/// אין הרשאה לפעולה.
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
