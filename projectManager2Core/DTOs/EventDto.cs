using projectManager2Core.Enums;

namespace projectManager2Core.DTOs;

public class EventDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public EventStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
