using System;

namespace AFS_Interview_Task.Exceptions;

public class TranslationTimeoutException : Exception
{
    public TranslationTimeoutException(string message = "The translation provider request timed out.") : base(message)
    {
    }
}