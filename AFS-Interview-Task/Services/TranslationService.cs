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
using Microsoft.Extensions.Options;

namespace AFS_Interview_Task.Services;

public class TranslationService : ITranslationService
{
    private readonly TranslatorProviderFactory _factory;
    private readonly ITranslationLogRepository _repository;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly TranslationExecutionOptions _executionOptions;

    public TranslationService(
        TranslatorProviderFactory factory,
        ITranslationLogRepository repository,
        ICorrelationIdAccessor correlationIdAccessor,
        IOptions<TranslationExecutionOptions> executionOptions)
    {
        _factory = factory;
        _repository = repository;
        _correlationIdAccessor = correlationIdAccessor;
        _executionOptions = executionOptions.Value;
    }

    public async Task<TranslateResponse> TranslateAsync(TranslateRequest request, CancellationToken ct)
    {
        var hasTranslator = !string.IsNullOrWhiteSpace(request.Translator);
        var provider = hasTranslator
            ? _factory.GetProvider(request.Translator!)
            : _factory.GetProviderByKey(_executionOptions.DefaultProvider);

        var effectiveTranslator = hasTranslator
            ? request.Translator!
            : provider.ProviderKey;

        var log = new TranslationLog
        {
            Translator = effectiveTranslator,
            InputText = request.Text,
            CorrelationId = _correlationIdAccessor.CorrelationId
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var translatedText = hasTranslator
                ? await provider.TranslateAsync(effectiveTranslator, request.Text, ct)
                : await provider.TranslateAsync(request.Text, ct);

            stopwatch.Stop();

            log.OutputText = translatedText;
            log.IsSuccess = true;
            log.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            log.ProviderStatusCode = 200;

            await _repository.AddAsync(log, ct);

            return new TranslateResponse(
                translatedText,
                effectiveTranslator,
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
            log.ProviderStatusCode = 408;
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
