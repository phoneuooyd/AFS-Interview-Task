using System.Collections.Generic;

namespace AFS_Interview_Task.Providers;

public class TranslatorRoutingOptions
{
    public Dictionary<string, string> Translators { get; set; } = new();
}
