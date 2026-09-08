using projectManager2Core.Enums;

namespace projectManager2Core.Models;

public class Task
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Available;

    /// <summary>
    /// Foreign Key ל-User - המשתמש שלקח את המשימה. null כאשר המשימה פנויה.
    /// </summary>
    public int? AssignedToUserId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? SortOrder { get; set; }

    /// <summary>
    /// Concurrency Token (נקודה 6 - מחליף את ה-ExecuteUpdateAsync הידני
    /// שהיה קודם ב-TryAssignAsync). לא Database-Generated במכוון: ל-Sqlite
    /// (המשמש בבדיקות) אין טיפוס rowversion/timestamp אוטומטי כמו ל-SQL
    /// Server, ולכן העמודה מנוהלת ומוגדלת ידנית באפליקציה בכל שמירה
    /// (ראו TaskRepository.SaveChangesAsync) - כך ההתנהגות זהה בדיוק בין
    /// שני ה-Providers. EF Core כולל את הערך שנטען במקור ב-WHERE של פקודת
    /// ה-UPDATE; אם שורה כבר השתנתה מאז שנטענה (משתמש אחר כבר עדכן/לקח
    /// אותה Task), ה-UPDATE לא פוגע באף שורה, ו-EF Core זורק
    /// DbUpdateConcurrencyException - בלי נעילה ידנית ובלי SQL גולמי.
    /// </summary>
    public int ConcurrencyVersion { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;

    public User? AssignedToUser { get; set; }
}
