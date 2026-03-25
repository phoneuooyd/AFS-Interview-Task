using System;
using System.Collections.Generic;
using System.Linq;
using AFS_Interview_Task.Exceptions;
using Microsoft.Extensions.Options;

namespace AFS_Interview_Task.Providers;

public class TranslatorProviderFactory
{
    private readonly IReadOnlyDictionary<string, ITranslatorProvider> _providersByKey;

    public string DefaultProviderName { get; }

    public TranslatorProviderFactory(IEnumerable<ITranslatorProvider> providers, IOptions<LeetSpeakTranslationOptions> options)
    {
        _providersByKey = providers.ToDictionary(p => Normalize(p.ProviderKey), StringComparer.OrdinalIgnoreCase);
        DefaultProviderName = Normalize(options.Value.Provider);
    }

    public ITranslatorProvider GetProvider(string? name)
    {
        var requestedProviderName = string.IsNullOrWhiteSpace(name)
            ? DefaultProviderName
            : Normalize(name);

        if (_providersByKey.TryGetValue(requestedProviderName, out var provider))
        {
            return provider;
        }

        throw new UnsupportedTranslatorException(name ?? requestedProviderName);
    }

    public ITranslatorProvider GetProviderByKey(string providerKey)
    {
        var normalizedProviderKey = Normalize(providerKey);

        if (_providersByKey.TryGetValue(normalizedProviderKey, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException(
            $"Provider '{providerKey}' is not registered in DI.");
    }

    public bool IsProviderRegistered(string? providerKey)
    {
        if (string.IsNullOrWhiteSpace(providerKey))
        {
            return false;
        }

        return _providersByKey.ContainsKey(Normalize(providerKey));
    }

    private static string Normalize(string value)
        => value.Trim().Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
}
