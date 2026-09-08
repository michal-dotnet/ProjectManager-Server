using System.ComponentModel.DataAnnotations;

namespace projectManager2Core.DTOs;

public class AddEventMemberDto
{
    [Required]
    public int UserId { get; set; }
}
