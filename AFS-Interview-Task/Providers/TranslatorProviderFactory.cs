using System;
using System.Collections.Generic;
using System.Linq;
using AFS_Interview_Task.Exceptions;

namespace AFS_Interview_Task.Providers;

public class TranslatorProviderFactory
{
    private readonly IReadOnlyDictionary<string, ITranslatorProvider> _providers;

    public TranslatorProviderFactory(IEnumerable<ITranslatorProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.TranslatorName, StringComparer.OrdinalIgnoreCase);
    }

    public ITranslatorProvider GetProvider(string name)
    {
        if (_providers.TryGetValue(name, out var provider))
        {
            return provider;
        }

        throw new UnsupportedTranslatorException(name);
    }
}