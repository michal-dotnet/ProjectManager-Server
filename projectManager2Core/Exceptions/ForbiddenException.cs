namespace projectManager2Core.Exceptions;

/// <summary>
/// המשתמש מאומת אך אינו מורשה לבצע את הפעולה (ממופה ל-403 Forbidden).
/// </summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
