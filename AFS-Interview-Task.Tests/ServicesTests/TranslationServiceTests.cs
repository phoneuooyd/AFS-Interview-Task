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
using Moq;
using Xunit;

namespace AFS_Interview_Task.Tests.ServicesTests;

public class TranslationServiceTests
{
    [Fact]
    public async Task TranslateAsync_WhenProviderSucceeds_ReturnsResponseAndPersistsSuccessLog()
    {
        var providerMock = new Mock<ITranslatorProvider>();
        providerMock.SetupGet(p => p.TranslatorName).Returns("pirate");
        providerMock.Setup(p => p.TranslateAsync("hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("ahoy");

        var repositoryMock = new Mock<ITranslationLogRepository>();
        var correlationId = Guid.NewGuid();
        var correlationAccessorMock = new Mock<ICorrelationIdAccessor>();
        correlationAccessorMock.SetupGet(c => c.CorrelationId).Returns(correlationId);

        var factory = new TranslatorProviderFactory(new List<ITranslatorProvider> { providerMock.Object });
        var sut = new TranslationService(factory, repositoryMock.Object, correlationAccessorMock.Object);

        var request = new TranslateRequest { Text = "hello", Translator = "pirate" };

        var result = await sut.TranslateAsync(request, CancellationToken.None);

        Assert.Equal("ahoy", result.TranslatedText);
        Assert.Equal("pirate", result.Translator);
        Assert.Equal(correlationId, result.CorrelationId);
        Assert.True(result.DurationMs >= 0);

        repositoryMock.Verify(r => r.AddAsync(
                It.Is<TranslationLog>(l =>
                    l.Translator == "pirate" &&
                    l.InputText == "hello" &&
                    l.OutputText == "ahoy" &&
                    l.IsSuccess &&
                    l.ProviderStatusCode == 200 &&
                    l.ErrorMessage == null &&
                    l.CorrelationId == correlationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_WhenProviderRateLimits_ThrowsAndPersistsFailureLog()
    {
        var providerMock = new Mock<ITranslatorProvider>();
        providerMock.SetupGet(p => p.TranslatorName).Returns("pirate");
        providerMock.Setup(p => p.TranslateAsync("hello", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RateLimitException("Too many requests"));

        var repositoryMock = new Mock<ITranslationLogRepository>();
        var correlationId = Guid.NewGuid();
        var correlationAccessorMock = new Mock<ICorrelationIdAccessor>();
        correlationAccessorMock.SetupGet(c => c.CorrelationId).Returns(correlationId);

        var factory = new TranslatorProviderFactory(new List<ITranslatorProvider> { providerMock.Object });
        var sut = new TranslationService(factory, repositoryMock.Object, correlationAccessorMock.Object);

        var request = new TranslateRequest { Text = "hello", Translator = "pirate" };

        await Assert.ThrowsAsync<RateLimitException>(() => sut.TranslateAsync(request, CancellationToken.None));

        repositoryMock.Verify(r => r.AddAsync(
                It.Is<TranslationLog>(l =>
                    l.Translator == "pirate" &&
                    l.InputText == "hello" &&
                    l.OutputText == null &&
                    !l.IsSuccess &&
                    l.ProviderStatusCode == 429 &&
                    l.ErrorMessage == "Too many requests" &&
                    l.CorrelationId == correlationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateAsync_WhenTranslatorIsUnsupported_ThrowsAndPersistsFailureLog()
    {
        var providerMock = new Mock<ITranslatorProvider>();
        providerMock.SetupGet(p => p.TranslatorName).Returns("pirate");

        var repositoryMock = new Mock<ITranslationLogRepository>();
        var correlationAccessorMock = new Mock<ICorrelationIdAccessor>();
        correlationAccessorMock.SetupGet(c => c.CorrelationId).Returns(Guid.NewGuid());

        var factory = new TranslatorProviderFactory(new List<ITranslatorProvider> { providerMock.Object });
        var sut = new TranslationService(factory, repositoryMock.Object, correlationAccessorMock.Object);

        var request = new TranslateRequest { Text = "hello", Translator = "unknown" };

        await Assert.ThrowsAsync<UnsupportedTranslatorException>(() => sut.TranslateAsync(request, CancellationToken.None));

        providerMock.Verify(p => p.TranslateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repositoryMock.Verify(r => r.AddAsync(
                It.Is<TranslationLog>(l =>
                    l.Translator == "unknown" &&
                    l.InputText == "hello" &&
                    l.OutputText == null &&
                    !l.IsSuccess &&
                    l.ProviderStatusCode == 400 &&
                    l.ErrorMessage == "The translator 'unknown' is not supported."),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
