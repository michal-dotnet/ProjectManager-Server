using System.ComponentModel.DataAnnotations;

namespace projectManager2Core.DTOs;

/// <summary>
/// שדות מותרים לעדכון בלבד. במכוון אין כאן AssignedToUserId/AssignedAt/Status -
/// הקצאת Task מתבצעת אך ורק דרך הפעולה העסקית הייעודית (Take Task).
/// </summary>
public class UpdateTaskDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? SortOrder { get; set; }
}
