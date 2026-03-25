using System.Threading;
using System.Threading.Tasks;

namespace AFS_Interview_Task.Providers;

public interface ITranslatorProvider
{
    string ProviderKey { get; }
    Task<string> TranslateAsync(string translator, string text, CancellationToken ct);
}
