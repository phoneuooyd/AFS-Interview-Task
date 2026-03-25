using System;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AFS_Interview_Task.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        if (exception is UnsupportedTranslatorException unsupportedEx)
        {
            problemDetails.Title = "Unsupported Translator";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Detail = unsupportedEx.Message;
        }
        else if (exception is RateLimitException rateLimitEx)
        {
            problemDetails.Title = "Rate Limit Exceeded";
            problemDetails.Status = StatusCodes.Status429TooManyRequests;
            problemDetails.Detail = rateLimitEx.Message;
            httpContext.Response.Headers["Retry-After"] = ((int)rateLimitEx.RetryAfter.TotalSeconds).ToString();
        }
        else if (exception is TranslationTimeoutException timeoutEx)
        {
            problemDetails.Title = "Gateway Timeout";
            problemDetails.Status = StatusCodes.Status504GatewayTimeout;
            problemDetails.Detail = timeoutEx.Message;
        }
        else if (exception is TranslationProviderException providerEx)
        {
            problemDetails.Title = "Bad Gateway";
            problemDetails.Status = StatusCodes.Status502BadGateway;
            problemDetails.Detail = providerEx.Message;
        }
        else
        {
            problemDetails.Title = "Internal Server Error";
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Detail = "An unexpected error occurred.";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}