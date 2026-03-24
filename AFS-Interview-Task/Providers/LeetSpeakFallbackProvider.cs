using System;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Providers.FunTranslations;
using Microsoft.Extensions.Options;

namespace AFS_Interview_Task.Providers;

public class LeetSpeakFallbackProvider : ITranslatorProvider
{
    private readonly FunTranslationsProvider _funTranslations;
    private readonly RapidApiLeetSpeakDecoderProvider _rapidApi;
    private readonly LeetSpeakTranslationOptions _options;

    public LeetSpeakFallbackProvider(
        FunTranslationsProvider funTranslations,
        RapidApiLeetSpeakDecoderProvider rapidApi,
        IOptions<LeetSpeakTranslationOptions> options)
    {
        _funTranslations = funTranslations;
        _rapidApi = rapidApi;
        _options = options.Value;
    }

    /// <summary>
    /// Leetspeak translation provider selected from configuration; the app routes requests to either FunTranslations or RapidAPI based on the configured backend.
    /// </summary>
    public string TranslatorName => "leetspeak";

    public Task<string> TranslateAsync(string text, CancellationToken ct)
    {
        return Normalize(_options.Provider) switch
        {
            "funtranslations" => _funTranslations.TranslateAsync(text, ct),
            "rapidapi" => _rapidApi.TranslateAsync(text, ct),
            "rapidapileetdecoder" => _rapidApi.TranslateAsync(text, ct),
            _ => throw new InvalidOperationException($"Unsupported leetspeak provider configured: '{_options.Provider}'. Use 'funtranslations' or 'rapidapi'.")
        };
    }

    private static string Normalize(string value)
        => value.Trim().Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
}
