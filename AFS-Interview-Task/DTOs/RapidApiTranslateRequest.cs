using System.ComponentModel.DataAnnotations;

namespace AFS_Interview_Task.DTOs;

public class RapidApiTranslateRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;
}
