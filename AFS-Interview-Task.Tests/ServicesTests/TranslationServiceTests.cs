using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Domain;
using AFS_Interview_Task.DTOs;
using AFS_Interview_Task.Exceptions;
using AFS_Interview_Task.Middleware;
using AFS_Interview_Task.Providers;
using AFS_Interview_Task.Repositories;
using AFS_Interview_Task.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AFS_Interview_Task.Tests.ServicesTests;

public class TranslationServiceTests
{
    private readonly Mock<ITranslatorProvider> _providerMock;
    private readonly Mock<ITranslationLogRepository> _repositoryMock;
    private readonly Mock<ICorrelationIdAccessor> _correlationIdAccessorMock;
    private readonly TranslatorProviderFactory _factory;
    private readonly TranslationService _sut;

    public TranslationServiceTests()
    {
        _providerMock = new Mock<ITranslatorProvider>();
        _providerMock.SetupGet(p => p.ProviderKey).Returns("rapidapi");

        _repositoryMock = new Mock<ITranslationLogRepository>();

        _correlationIdAccessorMock = new Mock<ICorrelationIdAccessor>();
        _correlationIdAccessorMock.SetupGet(c => c.CorrelationId).Returns(Guid.NewGuid());

        var routingOptions = Options.Create(new TranslatorRoutingOptions
        {
            Translators = new Dictionary<string, string>
            {
                ["leetspeak"] = "rapidapi"
            }
        });

        _factory = new TranslatorProviderFactory(new[] { _providerMock.Object }, routingOptions);

        _sut = new TranslationService(
            _factory,
            _repositoryMock.Object,
            _correlationIdAccessorMock.Object,
            Options.Create(new TranslationExecutionOptions { DefaultProvider = "rapidapi" }));
    }

    [Fact]
    public async Task GivenTranslatorInRequest_WhenProviderSucceeds_UsesTranslatorOverload()
    {
        var request = new TranslateRequest { Text = "hello", Translator = "leetspeak" };
        _providerMock.Setup(p => p.TranslateAsync("leetspeak", "hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("h3ll0");

        var result = await _sut.TranslateAsync(request, CancellationToken.None);

        result.TranslatedText.Should().Be("h3ll0");
        result.Translator.Should().Be("leetspeak");

        _providerMock.Verify(p => p.TranslateAsync("leetspeak", "hello", It.IsAny<CancellationToken>()), Times.Once);
        _providerMock.Verify(p => p.TranslateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenTranslatorMissing_WhenDefaultProviderSet_UsesProviderOverloadWithoutTranslator()
    {
        var request = new TranslateRequest { Text = "hello" };
        _providerMock.Setup(p => p.TranslateAsync("hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("h3ll0");

        var result = await _sut.TranslateAsync(request, CancellationToken.None);

        result.TranslatedText.Should().Be("h3ll0");
        result.Translator.Should().Be("rapidapi");

        _providerMock.Verify(p => p.TranslateAsync("hello", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenValidRequest_WhenProviderThrows429_LogsFailure_AndRethrows()
    {
        var request = new TranslateRequest { Text = "hello", Translator = "leetspeak" };
        _providerMock.Setup(p => p.TranslateAsync("leetspeak", "hello", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RateLimitException(TimeSpan.FromSeconds(10)));

        var act = async () => await _sut.TranslateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<RateLimitException>();

        _repositoryMock.Verify(r => r.AddAsync(It.Is<TranslationLog>(l =>
            !l.IsSuccess &&
            l.ProviderStatusCode == 429 &&
            l.OutputText == null &&
            l.ErrorMessage != null), It.IsAny<CancellationToken>()), Times.Once);
    }
}
