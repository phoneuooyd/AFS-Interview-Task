using System;

namespace AFS_Interview_Task.DTOs;

public record TranslationLogQuery(
    int Page = 1,
    int PageSize = 10,
    string? Translator = null,
    bool? IsSuccess = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? SearchText = null
);