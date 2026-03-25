using System;
using System.Collections.Generic;
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
    public void GivenTranslatorMappedToRegisteredProvider_ReturnsProvider()
    {
        var provider = new Mock<ITranslatorProvider>();
        provider.SetupGet(p => p.ProviderKey).Returns("rapidapi");

        var options = Options.Create(new TranslatorRoutingOptions
        {
            Translators = new Dictionary<string, string>
            {
                ["leetspeak"] = "rapidapi"
            }
        });

        var sut = new TranslatorProviderFactory(new[] { provider.Object }, options);

        var result = sut.GetProvider("leetspeak");

        result.Should().BeSameAs(provider.Object);
    }

    [Fact]
    public void GivenTranslatorNotConfigured_ThrowsUnsupportedTranslatorException()
    {
        var provider = new Mock<ITranslatorProvider>();
        provider.SetupGet(p => p.ProviderKey).Returns("rapidapi");

        var options = Options.Create(new TranslatorRoutingOptions());

        var sut = new TranslatorProviderFactory(new[] { provider.Object }, options);

        var act = () => sut.GetProvider("pirate");

        act.Should().Throw<UnsupportedTranslatorException>();
    }

    [Fact]
    public void GivenTranslatorMappedToMissingProvider_ThrowsInvalidOperationException()
    {
        var provider = new Mock<ITranslatorProvider>();
        provider.SetupGet(p => p.ProviderKey).Returns("funtranslations");

        var options = Options.Create(new TranslatorRoutingOptions
        {
            Translators = new Dictionary<string, string>
            {
                ["leetspeak"] = "rapidapi"
            }
        });

        var sut = new TranslatorProviderFactory(new[] { provider.Object }, options);

        var act = () => sut.GetProvider("leetspeak");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*rapidapi*");
    }

    [Fact]
    public void GivenProviderKey_WhenRegistered_ReturnsProvider()
    {
        var provider = new Mock<ITranslatorProvider>();
        provider.SetupGet(p => p.ProviderKey).Returns("rapidapi");

        var sut = new TranslatorProviderFactory(
            new[] { provider.Object },
            Options.Create(new TranslatorRoutingOptions()));

        var result = sut.GetProviderByKey("rapidapi");

        result.Should().BeSameAs(provider.Object);
    }

}
