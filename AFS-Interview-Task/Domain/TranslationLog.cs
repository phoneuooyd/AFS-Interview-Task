using System;

namespace AFS_Interview_Task.Domain;

public class TranslationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Translator { get; set; } = string.Empty;
    public string InputText { get; set; } = string.Empty;
    public string? OutputText { get; set; }
    public int? ProviderStatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
    public Guid CorrelationId { get; set; }
}