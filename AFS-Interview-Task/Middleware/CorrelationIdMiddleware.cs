using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AFS_Interview_Task.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var stringCorrelationId) &&
            Guid.TryParse(stringCorrelationId, out var correlationId))
        {
            correlationIdAccessor.SetCorrelationId(correlationId);
        }
        else
        {
            correlationId = Guid.NewGuid();
            correlationIdAccessor.SetCorrelationId(correlationId);
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId.ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }
}