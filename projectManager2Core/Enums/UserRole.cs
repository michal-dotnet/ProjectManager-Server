namespace projectManager2Core.Enums;

/// <summary>
/// Role גלובלי של המשתמש במערכת - נפרד לחלוטין מהיחס Creator/EventMember
/// שנבדק ברמת ה-Event הבודד (ראו EventAccessGuard/EventService), ואין בו
/// שום גישת-על ("Admin"): שני ה-Roles הם משתמשים רגילים לכל דבר, ההבדל
/// ביניהם הוא הרשאה עסקית אחת בלבד - מי רשאי ליצור Event (POST /api/events,
/// ראו [Authorize(Roles = "Manager")] ב-EventsController.Create).
///
/// Worker (ברירת המחדל, ערך 0): לא יכול ליצור Event. יכול להצטרף ל-Event
/// רק אם ה-Creator שלו הוסיף אותו כ-EventMember (ראו EventMembersController -
/// אין הצטרפות עצמית), ולקחת/להשלים Tasks בתוך Events שהוא חבר בהם.
///
/// Manager (ערך 1): בנוסף לכל מה שמותר ל-Worker, יכול גם ליצור Event חדש
/// (יש לו "תוכנית עבודה" משלו לנהל) ולהוסיף EventMembers ל-Events שהוא היוצר
/// שלהם.
///
/// המשתמש בוחר את ה-Role של עצמו בהרשמה (RegisterDto.Role) - זו החלטה
/// תפקידית, לא הרשאת-על, ולכן אין צורך במנגנון אישור/הענקה חיצוני.
/// </summary>
public enum UserRole
{
    Worker,
    Manager
}
