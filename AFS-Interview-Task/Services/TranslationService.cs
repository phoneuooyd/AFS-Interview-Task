using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Domain;
using AFS_Interview_Task.DTOs;
using AFS_Interview_Task.Exceptions;
using AFS_Interview_Task.Middleware;
using AFS_Interview_Task.Providers;
using AFS_Interview_Task.Repositories;

namespace AFS_Interview_Task.Services;

public class TranslationService : ITranslationService
{
    private readonly TranslatorProviderFactory _factory;
    private readonly ITranslationLogRepository _repository;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public TranslationService(
        TranslatorProviderFactory factory, 
        ITranslationLogRepository repository,
        ICorrelationIdAccessor correlationIdAccessor)
    {
        _factory = factory;
        _repository = repository;
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task<TranslateResponse> TranslateAsync(TranslateRequest request, CancellationToken ct)
    {
        var provider = _factory.GetProvider(request.Translator);
        
        var log = new TranslationLog
        {
            Translator = request.Translator,
            InputText = request.Text,
            CorrelationId = _correlationIdAccessor.CorrelationId
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var translatedText = await provider.TranslateAsync(request.Text, ct);
            
            stopwatch.Stop();
            
            log.OutputText = translatedText;
            log.IsSuccess = true;
            log.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            log.ProviderStatusCode = 200;

            await _repository.AddAsync(log, ct);

            return new TranslateResponse(
                translatedText,
                request.Translator,
                _correlationIdAccessor.CorrelationId,
                log.DurationMs
            );
        }
        catch (RateLimitException ex)
        {
            stopwatch.Stop();
            log.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            log.IsSuccess = false;
            log.ErrorMessage = ex.Message;
            log.ProviderStatusCode = 429;
            await _repository.AddAsync(log, ct);
            throw;
        }
        catch (TranslationTimeoutException ex)
        {
            stopwatch.Stop();
            log.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            log.IsSuccess = false;
            log.ErrorMessage = ex.Message;
            log.ProviderStatusCode = 408; // Timeout
            await _repository.AddAsync(log, ct);
            throw;
        }
        catch (TranslationProviderException ex)
        {
            stopwatch.Stop();
            log.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            log.IsSuccess = false;
            log.ErrorMessage = ex.Message;
            log.ProviderStatusCode = ex.StatusCode;
            await _repository.AddAsync(log, ct);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            log.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            log.IsSuccess = false;
            log.ErrorMessage = ex.Message;
            log.ProviderStatusCode = 500;
            await _repository.AddAsync(log, ct);
            throw;
        }
    }

    public async Task<PagedResult<TranslationLogDto>> GetLogsAsync(TranslationLogQuery query, CancellationToken ct)
    {
        return await _repository.QueryAsync(query, ct);
    }
}