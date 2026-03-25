using System;
using AFS_Interview_Task.Exceptions;
using AFS_Interview_Task.Providers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AFS_Interview_Task.Tests.ProvidersTests;

public class TranslatorProviderFactoryTests
{
    [Fact]
    public void GivenNullProviderName_ReturnsDefaultProvider()
    {
        var provider = new Mock<ITranslatorProvider>();
        provider.SetupGet(p => p.ProviderKey).Returns("rapidapi");

        var options = Options.Create(new LeetSpeakTranslationOptions
        {
            Provider = "rapidapi"
        });

        var sut = new TranslatorProviderFactory(new[] { provider.Object }, options);

        var result = sut.GetProvider(null);

        result.Should().BeSameAs(provider.Object);
    }

    [Fact]
    public void GivenUnknownProvider_ThrowsUnsupportedTranslatorException()
    {
        var provider = new Mock<ITranslatorProvider>();
        provider.SetupGet(p => p.ProviderKey).Returns("rapidapi");

        var options = Options.Create(new LeetSpeakTranslationOptions
        {
            Provider = "rapidapi"
        });

        var sut = new TranslatorProviderFactory(new[] { provider.Object }, options);

        var act = () => sut.GetProvider("pirate");

        act.Should().Throw<UnsupportedTranslatorException>();
    }

    [Fact]
    public void GivenDefaultProviderMissingInDi_ThrowsUnsupportedTranslatorException()
    {
        var provider = new Mock<ITranslatorProvider>();
        provider.SetupGet(p => p.ProviderKey).Returns("funtranslations");

        var options = Options.Create(new LeetSpeakTranslationOptions
        {
            Provider = "rapidapi"
        });

        var sut = new TranslatorProviderFactory(new[] { provider.Object }, options);

        var act = () => sut.GetProvider(null);

        act.Should().Throw<UnsupportedTranslatorException>();
    }

    [Fact]
    public void GivenRegisteredProviderKey_ReturnsProvider()
    {
        var provider = new Mock<ITranslatorProvider>();
        provider.SetupGet(p => p.ProviderKey).Returns("rapidapi");

        var options = Options.Create(new LeetSpeakTranslationOptions
        {
            Provider = "rapidapi"
        });

        var sut = new TranslatorProviderFactory(new[] { provider.Object }, options);

        var result = sut.GetProviderByKey("rapidapi");

        result.Should().BeSameAs(provider.Object);
    }

    [Fact]
    public void IsProviderRegistered_ReturnsFalseForNullOrUnknownProvider()
    {
        var provider = new Mock<ITranslatorProvider>();
        provider.SetupGet(p => p.ProviderKey).Returns("rapidapi");

        var options = Options.Create(new LeetSpeakTranslationOptions
        {
            Provider = "rapidapi"
        });

        var sut = new TranslatorProviderFactory(new[] { provider.Object }, options);

        sut.IsProviderRegistered(null).Should().BeFalse();
        sut.IsProviderRegistered("missing").Should().BeFalse();
        sut.IsProviderRegistered("rapidapi").Should().BeTrue();
    }
}
