namespace projectManager2Core.Exceptions;

/// <summary>
/// התנגשות עסקית או Concurrency (ממופה ל-409 Conflict) - לדוגמה: Task שכבר
/// נלקחה, Event שכבר ב-Completed, Email כפול, הסרת EventMember שמחזיק Task.
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }
}
