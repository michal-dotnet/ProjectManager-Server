using Microsoft.AspNetCore.Mvc;
using projectManager2Core.Exceptions;

namespace projectManager2.Middlewares;

/// <summary>
/// Middleware גלובלי שתופס כל חריגה שנזרקת בהמשך ה-Pipeline, וממפה אותה
/// ל-ProblemDetails עקבי עם קוד HTTP מתאים. חריגות עסקיות מוכרות (Not
/// Found/Forbidden/Conflict/BadRequest/Unauthorized) נחשפות ללקוח עם הודעה
/// ברורה; כל חריגה אחרת (בלתי צפויה) מוחזרת כ-500 עם הודעה גנרית בלבד,
/// בלי Stack Trace או פרטי מימוש.
///
/// כל חריגה שנתפסת כאן נרשמת גם ל-Log: התנגשות על משאב (ConflictException,
/// 409) ופרטי התחברות שגויים (UnauthorizedException, 401) ברמת Warning -
/// כי אלה מצבים חריגים אך צפויים, שכבר טופלו כהלכה ע"י ה-Business Logic;
/// כל חריגה אחרת (כולל 500 בלתי צפוי) ברמת Error. ה-CorrelationId שנוסף
/// ל-Scope ב-CorrelationIdMiddleware מופיע אוטומטית בכל שורת Log כזו.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        LogException(context, exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "אירעה שגיאה בלתי צפויה בשרת."
                : exception.Message,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private void LogException(HttpContext context, Exception exception)
    {
        // התנגשות על משאב ופרטי התחברות שגויים הם מצבים חריגים אך צפויים,
        // שכבר מטופלים נכון ע"י ה-Business Logic - Warning, לא Error. כל
        // חריגה אחרת שמגיעה לכאן - Error. שימו לב: exception.Message בכל
        // המקרים כאן הוא תמיד טקסט קבוע (לא כולל סיסמה/Token/תוכן בקשה) -
        // ראו AuthService.
        if (exception is ConflictException or UnauthorizedException)
        {
            _logger.LogWarning(
                exception,
                "חריגה עסקית צפויה במהלך {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                exception.Message);
        }
        else
        {
            _logger.LogError(
                exception,
                "חריגה נתפסה במהלך {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
    }
}
