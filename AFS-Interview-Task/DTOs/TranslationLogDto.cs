using System;

namespace AFS_Interview_Task.DTOs;

public record TranslationLogDto(
    Guid Id,
    DateTime CreatedAtUtc,
    string Translator,
    string InputText,
    string? OutputText,
    int? ProviderStatusCode,
    bool IsSuccess,
    string? ErrorMessage,
    int DurationMs,
    Guid CorrelationId
);