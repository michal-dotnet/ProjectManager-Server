using projectManager2Core.Enums;

namespace projectManager2Core.DTOs;

public class UserDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; }
}
