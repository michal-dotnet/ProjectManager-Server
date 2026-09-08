using System.ComponentModel.DataAnnotations;

namespace projectManager2Core.DTOs;

public class CreateTaskDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? SortOrder { get; set; }
}
