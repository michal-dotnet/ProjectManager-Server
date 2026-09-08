using projectManager2Core.Enums;

namespace projectManager2Core.DTOs;

public class TaskDto
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskStatusEnum Status { get; set; }

    public int? AssignedToUserId { get; set; }

    public string? AssignedToUserName { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? SortOrder { get; set; }
}
