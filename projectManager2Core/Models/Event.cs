using projectManager2Core.Enums;

namespace projectManager2Core.Models;

public class Event
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// המשתמש שיצר את ה-Event והוא היחיד המורשה לנהל אותו (Event Creator).
    /// </summary>
    public int CreatedByUserId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Draft;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;

    public ICollection<EventMember> Members { get; set; } = new List<EventMember>();

    public ICollection<Task> Tasks { get; set; } = new List<Task>();
}
