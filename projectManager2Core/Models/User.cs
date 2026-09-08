using projectManager2Core.Enums;

namespace projectManager2Core.Models;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// חייב להיות Unique ברמת ה-Database (ראו הגדרת האינדקס ב-DataContext).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash של הסיסמה (Microsoft.AspNetCore.Identity.PasswordHasher) -
    /// לעולם לא נשמרת סיסמה בטקסט גלוי. נקבע רק דרך AuthService.RegisterAsync.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Role גלובלי (ראו UserRole) - Manager/Worker, נבחר ע"י המשתמש עצמו
    /// בהרשמה (RegisterDto.Role). אין Role בעל גישת-על ("Admin").
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Worker;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();

    public ICollection<EventMember> EventMemberships { get; set; } = new List<EventMember>();

    public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
}
