using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.DTOs;

namespace AFS_Interview_Task.Services;

public interface ITranslationService
{
    Task<TranslateResponse> TranslateAsync(TranslateRequest request, CancellationToken ct);
    Task<PagedResult<TranslationLogDto>> GetLogsAsync(TranslationLogQuery query, CancellationToken ct);
}