using System.Collections.Generic;

namespace AFS_Interview_Task.Providers;

public class LeetSpeakTranslationOptions
{
    public string Provider { get; set; } = "rapidapi";
    public Dictionary<string, ProviderProfileOptions> Providers { get; set; } = new();
}

public class ProviderProfileOptions
{
    public bool RequiresTranslator { get; set; }
    public string? DefaultTranslator { get; set; }
}
