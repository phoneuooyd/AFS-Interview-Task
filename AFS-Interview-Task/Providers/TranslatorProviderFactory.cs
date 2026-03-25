using System;
using System.Collections.Generic;
using System.Linq;
using AFS_Interview_Task.Exceptions;
using Microsoft.Extensions.Options;

namespace AFS_Interview_Task.Providers;

public class TranslatorProviderFactory
{
    private readonly IReadOnlyDictionary<string, ITranslatorProvider> _providersByKey;
    private readonly IReadOnlyDictionary<string, string> _translatorRoutes;

    public TranslatorProviderFactory(IEnumerable<ITranslatorProvider> providers, IOptions<TranslatorRoutingOptions> options)
    {
        _providersByKey = providers.ToDictionary(p => Normalize(p.ProviderKey), StringComparer.OrdinalIgnoreCase);

        _translatorRoutes = options.Value.Translators
            .ToDictionary(
                route => Normalize(route.Key),
                route => Normalize(route.Value),
                StringComparer.OrdinalIgnoreCase);
    }

    public ITranslatorProvider GetProvider(string translator)
    {
        var normalizedTranslator = Normalize(translator);

        if (!_translatorRoutes.TryGetValue(normalizedTranslator, out var providerKey))
        {
            throw new UnsupportedTranslatorException(translator);
        }

        return GetProviderByKey(providerKey, translator);
    }

    public ITranslatorProvider GetProviderByKey(string providerKey, string? translatorContext = null)
    {
        var normalizedProviderKey = Normalize(providerKey);

        if (_providersByKey.TryGetValue(normalizedProviderKey, out var provider))
        {
            return provider;
        }

        var translatorLabel = string.IsNullOrWhiteSpace(translatorContext)
            ? "n/a"
            : translatorContext;

        throw new InvalidOperationException(
            $"Provider '{providerKey}' configured for translator '{translatorLabel}' is not registered in DI.");
    }

    private static string Normalize(string value)
        => value.Trim().Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
}
