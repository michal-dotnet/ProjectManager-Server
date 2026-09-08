namespace projectManager2Core.Exceptions;

/// <summary>
/// Resource לא קיים (ממופה ל-404 Not Found).
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException For(string resourceName, object key)
        => new($"{resourceName} עם המזהה '{key}' לא נמצא.");
}
