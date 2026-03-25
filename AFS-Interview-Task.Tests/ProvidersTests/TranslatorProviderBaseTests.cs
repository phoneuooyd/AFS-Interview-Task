using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Exceptions;
using AFS_Interview_Task.Providers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AFS_Interview_Task.Tests.ProvidersTests;

public class TranslatorProviderBaseTests
{
    [Fact]
    public async Task GivenTranslatorRequiredAndMissing_ThrowsUnsupportedTranslatorException()
    {
        var sut = new FakeProvider(
            "funtranslations",
            Options.Create(new LeetSpeakTranslationOptions
            {
                Providers =
                {
                    ["funtranslations"] = new ProviderProfileOptions { RequiresTranslator = true }
                }
            }));

        var act = async () => await sut.TranslateAsync(string.Empty, "hello", CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedTranslatorException>();
    }

    [Fact]
    public async Task GivenDefaultTranslatorConfigured_UsesDefaultWhenTranslatorIsMissing()
    {
        var sut = new FakeProvider(
            "rapidapi",
            Options.Create(new LeetSpeakTranslationOptions
            {
                Providers =
                {
                    ["rapidapi"] = new ProviderProfileOptions { DefaultTranslator = "leetspeak" }
                }
            }));

        var result = await sut.TranslateAsync(string.Empty, "1337", CancellationToken.None);

        result.Should().Be("leetspeak:1337");
    }

    private sealed class FakeProvider : TranslatorProviderBase
    {
        private readonly string _providerKey;

        public FakeProvider(string providerKey, IOptions<LeetSpeakTranslationOptions> options)
            : base(options, providerKey)
        {
            _providerKey = providerKey;
        }

        public override string ProviderKey => _providerKey;

        protected override Task<string> ExecuteCoreAsync(string translator, string text, CancellationToken ct)
            => Task.FromResult($"{translator}:{text}");
    }
}
