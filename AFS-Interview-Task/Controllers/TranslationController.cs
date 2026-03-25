using System;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.DTOs;
using AFS_Interview_Task.Providers;
using AFS_Interview_Task.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AFS_Interview_Task.Controllers;

[ApiController]
[Route("api")]
public class TranslationController : ControllerBase
{
    private readonly ITranslationService _translationService;
    private readonly TranslationExecutionOptions _executionOptions;

    public TranslationController(
        ITranslationService translationService,
        IOptions<TranslationExecutionOptions> executionOptions)
    {
        _translationService = translationService;
        _executionOptions = executionOptions.Value;
    }

    [HttpPost("translate")]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request, CancellationToken ct)
    {
        if (IsTranslatorRequiredByDefaultProvider() && string.IsNullOrWhiteSpace(request.Translator))
        {
            ModelState.AddModelError(nameof(request.Translator),
                "Field 'translator' is required when the default provider is FunTranslations.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _translationService.TranslateAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("translation-logs")]
    public async Task<IActionResult> GetLogs([FromQuery] TranslationLogQuery query, CancellationToken ct)
    {
        if (query.PageSize > 100)
        {
            query = query with { PageSize = 100 };
        }

        if (query.Page < 1)
        {
            query = query with { Page = 1 };
        }

        if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc > query.ToUtc)
        {
            return BadRequest("FromUtc cannot be later than ToUtc.");
        }

        var result = await _translationService.GetLogsAsync(query, ct);
        return Ok(result);
    }

    private bool IsTranslatorRequiredByDefaultProvider()
        => string.Equals(_executionOptions.DefaultProvider, "funtranslations", StringComparison.OrdinalIgnoreCase);
}
