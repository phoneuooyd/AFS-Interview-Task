using System;

namespace AFS_Interview_Task.Exceptions;

public class UnsupportedTranslatorException : Exception
{
    public UnsupportedTranslatorException(string translator) 
        : base($"The translator '{translator}' is not supported.")
    {
    }
}