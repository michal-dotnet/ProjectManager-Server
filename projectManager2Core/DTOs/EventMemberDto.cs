namespace projectManager2Core.DTOs;

public class EventMemberDto
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }
}
