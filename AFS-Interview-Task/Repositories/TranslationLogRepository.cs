using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Domain;
using AFS_Interview_Task.DTOs;
using AFS_Interview_Task.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AFS_Interview_Task.Repositories;

public class TranslationLogRepository : ITranslationLogRepository
{
    private readonly AppDbContext _dbContext;

    public TranslationLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TranslationLog log, CancellationToken ct)
    {
        _dbContext.TranslationLogs.Add(log);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<TranslationLogDto>> QueryAsync(TranslationLogQuery query, CancellationToken ct)
    {
        var q = _dbContext.TranslationLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Translator))
        {
            q = q.Where(l => l.Translator == query.Translator);
        }

        if (query.IsSuccess.HasValue)
        {
            q = q.Where(l => l.IsSuccess == query.IsSuccess.Value);
        }

        if (query.FromUtc.HasValue)
        {
            q = q.Where(l => l.CreatedAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            q = q.Where(l => l.CreatedAtUtc <= query.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            q = q.Where(l => l.InputText.Contains(query.SearchText));
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new TranslationLogDto(
                l.Id,
                l.CreatedAtUtc,
                l.Translator,
                l.InputText,
                l.OutputText,
                l.ProviderStatusCode,
                l.IsSuccess,
                l.ErrorMessage,
                l.DurationMs,
                l.CorrelationId
            ))
            .ToListAsync(ct);

        return new PagedResult<TranslationLogDto>(
            totalCount,
            query.Page,
            query.PageSize,
            items
        );
    }
}