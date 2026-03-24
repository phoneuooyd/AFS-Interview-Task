using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Domain;
using AFS_Interview_Task.DTOs;

namespace AFS_Interview_Task.Repositories;

public interface ITranslationLogRepository
{
    Task AddAsync(TranslationLog log, CancellationToken ct);
    Task<PagedResult<TranslationLogDto>> QueryAsync(TranslationLogQuery query, CancellationToken ct);
}