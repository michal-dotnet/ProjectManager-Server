using projectManager2Core.Enums;

namespace projectManager2Core.Services;

/// <summary>
/// חושף את זהות המשתמש המאומת עבור הבקשה הנוכחית, כולל ה-Role שבחר
/// בהרשמה (Manager/Worker - נבדק דרך [Authorize(Roles=...)] על ה-Controllers,
/// למשל EventsController.Create). המימוש (התלוי ב-HttpContext.User, שה-JWT
/// Middleware ממלא לאחר אימות מוצלח) נמצא בשכבת ה-Api, כדי ש-Core יישאר
/// נקי מתלות ב-ASP.NET Core.
/// </summary>
public interface ICurrentUserService
{
    int UserId { get; }

    UserRole Role { get; }
}
