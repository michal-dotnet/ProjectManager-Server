namespace projectManager2Core.Exceptions;

/// <summary>
/// בקשה לא תקינה ברמה עסקית שאינה נתפסת על ידי Model Validation הרגיל
/// (ממופה ל-400 Bad Request).
/// </summary>
public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message)
    {
    }
}
