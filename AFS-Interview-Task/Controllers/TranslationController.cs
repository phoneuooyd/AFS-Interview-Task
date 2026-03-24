using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.DTOs;
using AFS_Interview_Task.Services;
using Microsoft.AspNetCore.Mvc;

namespace AFS_Interview_Task.Controllers;

[ApiController]
[Route("api")]
public class TranslationController : ControllerBase
{
    private readonly ITranslationService _translationService;

    public TranslationController(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    [HttpPost("translate")]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request, CancellationToken ct)
    {
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
        // Add minimal validation for query size to prevent abusive sizes
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
}