using System;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Exceptions;
using Microsoft.Extensions.Options;

namespace AFS_Interview_Task.Providers;

public abstract class TranslatorProviderBase : ITranslatorProvider
{
    private readonly ProviderProfileOptions _profile;

    public abstract string ProviderKey { get; }

    protected TranslatorProviderBase(IOptions<LeetSpeakTranslationOptions> options, string providerKey)
    {
        _profile = ResolveProfile(options, providerKey);
    }

    public async Task<string> TranslateAsync(string translator, string text, CancellationToken ct)
    {
        var normalizedText = NormalizeText(text);
        var resolvedTranslator = ResolveTranslator(translator);

        ValidateTranslator(resolvedTranslator);

        return await ExecuteCoreAsync(resolvedTranslator, normalizedText, ct);
    }

    protected virtual string NormalizeText(string text)
        => text.Trim();

    protected virtual void ValidateTranslator(string translator)
    {
    }

    protected abstract Task<string> ExecuteCoreAsync(string translator, string text, CancellationToken ct);

    private string ResolveTranslator(string translator)
    {
        if (!string.IsNullOrWhiteSpace(translator))
        {
            return translator.Trim();
        }

        if (!string.IsNullOrWhiteSpace(_profile.DefaultTranslator))
        {
            return _profile.DefaultTranslator.Trim();
        }

        if (_profile.RequiresTranslator)
        {
            throw new UnsupportedTranslatorException($"translator value is required for provider '{ProviderKey}'");
        }

        return string.Empty;
    }

    private static ProviderProfileOptions ResolveProfile(IOptions<LeetSpeakTranslationOptions> options, string providerKey)
    {
        foreach (var profile in options.Value.Providers)
        {
            if (string.Equals(profile.Key, providerKey, StringComparison.OrdinalIgnoreCase))
            {
                return profile.Value;
            }
        }

        return new ProviderProfileOptions();
    }
}
