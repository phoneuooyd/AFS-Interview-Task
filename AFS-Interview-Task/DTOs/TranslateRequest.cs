using System.ComponentModel.DataAnnotations;

namespace AFS_Interview_Task.DTOs;

public class TranslateRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Translator { get; set; } = string.Empty;
}
