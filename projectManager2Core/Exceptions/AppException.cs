namespace projectManager2Core.Exceptions;

/// <summary>
/// בסיס משותף לכל חריגות הדומיין/העסק שהמערכת זורקת במכוון, כדי שה-
/// Global Exception Handling Middleware ב-Api יוכל למפות אותן לקוד HTTP
/// עקבי דרך ProblemDetails, בלי לחשוף Stack Trace ללקוח.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }
}
