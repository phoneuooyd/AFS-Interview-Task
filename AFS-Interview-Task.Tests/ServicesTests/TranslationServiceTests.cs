using System;
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

        var options = Options.Create(new LeetSpeakTranslationOptions
        {
            Provider = "rapidapi"
        });

        _factory = new TranslatorProviderFactory(new[] { _providerMock.Object }, options);

        _sut = new TranslationService(
            _factory,
            _repositoryMock.Object,
            _correlationIdAccessorMock.Object);
    }

    [Fact]
    public async Task GivenValidRequest_WhenProviderSucceeds_ReturnsTranslatedText()
    {
        var request = new TranslateRequest { Text = "1337", Translator = "leetspeak" };
        _providerMock.Setup(p => p.TranslateAsync("leetspeak", "1337", It.IsAny<CancellationToken>()))
            .ReturnsAsync("leet");

        var result = await _sut.TranslateAsync(request, CancellationToken.None);

        result.TranslatedText.Should().Be("leet");
    }

    [Fact]
    public async Task GivenValidRequest_WhenProviderSucceeds_ReturnsTranslatedText_AndLogsSuccess()
    {
        var request = new TranslateRequest { Text = "hello", Translator = "leetspeak" };
        _providerMock.Setup(p => p.TranslateAsync("leetspeak", "hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("h3ll0");

        var result = await _sut.TranslateAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.TranslatedText.Should().Be("h3ll0");
        result.Translator.Should().Be("leetspeak");

        _repositoryMock.Verify(r => r.AddAsync(It.Is<TranslationLog>(l =>
            l.IsSuccess &&
            l.ProviderStatusCode == 200 &&
            l.OutputText == "h3ll0" &&
            l.InputText == "hello"), It.IsAny<CancellationToken>()), Times.Once);
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


    [Fact]
    public async Task GivenValidRequest_WhenProviderTimeouts_LogsFailure_AndRethrows()
    {
        var request = new TranslateRequest { Text = "hello", Translator = "leetspeak" };
        _providerMock.Setup(p => p.TranslateAsync("leetspeak", "hello", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TranslationTimeoutException());

        var act = async () => await _sut.TranslateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<TranslationTimeoutException>();

        _repositoryMock.Verify(r => r.AddAsync(It.Is<TranslationLog>(l =>
            !l.IsSuccess &&
            l.ProviderStatusCode == 504 &&
            l.OutputText == null &&
            l.ErrorMessage != null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenValidRequest_WhenProviderReturnsNon200_LogsFailure_AndRethrows()
    {
        var request = new TranslateRequest { Text = "hello", Translator = "leetspeak" };
        _providerMock.Setup(p => p.TranslateAsync("leetspeak", "hello", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TranslationProviderException(502, "Bad Gateway from provider"));

        var act = async () => await _sut.TranslateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<TranslationProviderException>();

        _repositoryMock.Verify(r => r.AddAsync(It.Is<TranslationLog>(l =>
            !l.IsSuccess &&
            l.ProviderStatusCode == 502 &&
            l.OutputText == null &&
            l.ErrorMessage == "Bad Gateway from provider"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenTooLongInput_WhenProviderSucceeds_LogsTrimmedInput()
    {
        var tooLongInput = new string('a', 600);
        var request = new TranslateRequest { Text = tooLongInput, Translator = "leetspeak" };

        _providerMock.Setup(p => p.TranslateAsync("leetspeak", tooLongInput, It.IsAny<CancellationToken>()))
            .ReturnsAsync("translated");

        await _sut.TranslateAsync(request, CancellationToken.None);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<TranslationLog>(l =>
            l.IsSuccess &&
            l.InputText.Length == 500 &&
            l.InputText == tooLongInput[..500]), It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task GivenProviderOverride_WhenProviderSucceeds_ReturnsTranslatedText()
    {
        _providerMock.Setup(p => p.TranslateAsync("leetspeak", "abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync("4bc");

        var result = await _sut.TranslateAsync("rapidapi", "leetspeak", "abc", CancellationToken.None);

        result.TranslatedText.Should().Be("4bc");
        result.Translator.Should().Be("leetspeak");
    }
}
