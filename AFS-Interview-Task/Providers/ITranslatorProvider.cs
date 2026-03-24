using System.Threading;
using System.Threading.Tasks;

namespace AFS_Interview_Task.Providers;

public interface ITranslatorProvider
{
    string TranslatorName { get; }
    Task<string> TranslateAsync(string text, CancellationToken ct);
}