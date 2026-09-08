using System.ComponentModel.DataAnnotations;
using projectManager2Core.Enums;

namespace projectManager2Core.DTOs;

public class RegisterDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// המשתמש בוחר בעצמו בהרשמה האם הוא רוצה להיות Manager (יכול ליצור
    /// Events) או Worker (רק מצטרף ל-Events כשמוזמן ולוקח Tasks). אין כאן
    /// שום הרשאת-על - זו רק בחירת תפקיד, ולכן אין בעיה לתת למשתמש לבחור
    /// אותה בעצמו. אם השדה לא נשלח, ברירת המחדל היא Worker (הערך הראשון
    /// ב-enum) - התפקיד הפחות-מורשה.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Worker;
}
