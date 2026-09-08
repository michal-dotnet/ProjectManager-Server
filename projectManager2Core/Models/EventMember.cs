namespace projectManager2Core.Models;

/// <summary>
/// מגדיר מי רשאי להיכנס ולעבוד בתוך Event. אין הרשאות ברמת Task -
/// כל EventMember יכול לקחת כל Task פנויה בתוך ה-Event שהוא חבר בו.
/// </summary>
public class EventMember
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    // Navigation properties
    public Event Event { get; set; } = null!;

    public User User { get; set; } = null!;
}
