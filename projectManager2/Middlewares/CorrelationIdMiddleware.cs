namespace projectManager2.Middlewares;

/// <summary>
/// Middleware ראשון בפייפליין (רשום ב-Program.cs לפני הכל): יוצר, או קורא
/// אם כבר קיים ב-Header, CorrelationId לכל בקשה נכנסת, ומצרף אותו ל-Scope
/// של ה-ILogger. כתוצאה מכך כל שורת Log שנכתבת בהמשך הבקשה - בכל שכבה,
/// כולל בתוך ExceptionHandlingMiddleware - כוללת אוטומטית את אותו
/// CorrelationId, בלי שצריך להעביר אותו ידנית מ-Method ל-Method.
///
/// הקוד כאן משתמש רק ב-ILogger הרגיל של Microsoft.Extensions.Logging
/// (BeginScope) - NLog (המוגדר ב-Program.cs וב-nlog.config) הוא זה שקורא
/// בפועל את ה-Scope הזה דרך ${scopeproperty:item=CorrelationId}. כך אפשר
/// להחליף ספריית Logging בעתיד בלי לגעת ב-Middleware הזה.
///
/// שימו לב: כאן נרשמים אך ורק Method, Path וה-CorrelationId - לעולם לא
/// Body של הבקשה, סיסמאות, Tokens או תוכן טופס הרשמה. קובץ Log הוא קובץ
/// שאנשים קוראים, ואסור שיחשוף מידע רגיש.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";
    public const string CorrelationIdItemsKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[CorrelationIdItemsKey] = correlationId;
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            _logger.LogInformation(
                "בקשה נכנסת: {Method} {Path} (CorrelationId={CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            await _next(context);

            _logger.LogDebug(
                "בקשה הסתיימה: {Method} {Path} -> {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var existing) &&
            !string.IsNullOrWhiteSpace(existing))
        {
            return existing.ToString()!;
        }

        return Guid.NewGuid().ToString();
    }
}
