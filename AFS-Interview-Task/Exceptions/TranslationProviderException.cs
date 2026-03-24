using System;

namespace AFS_Interview_Task.Exceptions;

public class TranslationProviderException : Exception
{
    public int StatusCode { get; }

    public TranslationProviderException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}