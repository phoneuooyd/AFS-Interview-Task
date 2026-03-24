using System;

namespace AFS_Interview_Task.Middleware;

public interface ICorrelationIdAccessor
{
    Guid CorrelationId { get; }
    void SetCorrelationId(Guid correlationId);
}

public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public Guid CorrelationId { get; private set; }

    public void SetCorrelationId(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}