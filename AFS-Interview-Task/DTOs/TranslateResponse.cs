using System;

namespace AFS_Interview_Task.DTOs;

public record TranslateResponse(
    string TranslatedText,
    string Translator,
    Guid RequestId,
    int DurationMs
);