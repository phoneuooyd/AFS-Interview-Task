using System;

namespace AFS_Interview_Task.Exceptions;

public class RateLimitException : Exception
{
    public TimeSpan RetryAfter { get; }

    public RateLimitException(TimeSpan retryAfter, string message = "Rate limit exceeded.") : base(message)
    {
        RetryAfter = retryAfter;
    }
}