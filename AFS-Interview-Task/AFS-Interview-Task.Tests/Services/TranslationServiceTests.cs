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
using Moq;
using Xunit;

namespace AFS_Interview_Task.Tests.Services;

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
        _providerMock.SetupGet(p => p.TranslatorName).Returns("leetspeak");

        _repositoryMock = new Mock<ITranslationLogRepository>();
        
        _correlationIdAccessorMock = new Mock<ICorrelationIdAccessor>();
        _correlationIdAccessorMock.SetupGet(c => c.CorrelationId).Returns(Guid.NewGuid());

        _factory = new TranslatorProviderFactory(new[] { _providerMock.Object });

        _sut = new TranslationService(_factory, _repositoryMock.Object, _correlationIdAccessorMock.Object);
    }

    [Fact]
    public async Task GivenValidRequest_WhenProviderSucceeds_ReturnsTranslatedText_AndLogsSuccess()
    {
        // Arrange
        var request = new TranslateRequest { Text = "hello", Translator = "leetspeak" };
        _providerMock.Setup(p => p.TranslateAsync("hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("h3ll0");

        // Act
        var result = await _sut.TranslateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TranslatedText.Should().Be("h3ll0");
        result.Translator.Should().Be("leetspeak");

        _repositoryMock.Verify(r => r.AddAsync(It.Is<TranslationLog>(l => 
            l.IsSuccess == true &&
            l.ProviderStatusCode == 200 &&
            l.OutputText == "h3ll0" &&
            l.InputText == "hello"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenValidRequest_WhenProviderThrows429_LogsFailure_AndRethrows()
    {
        // Arrange
        var request = new TranslateRequest { Text = "hello", Translator = "leetspeak" };
        _providerMock.Setup(p => p.TranslateAsync("hello", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RateLimitException(TimeSpan.FromSeconds(10)));

        // Act
        var act = async () => await _sut.TranslateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RateLimitException>();

        _repositoryMock.Verify(r => r.AddAsync(It.Is<TranslationLog>(l => 
            l.IsSuccess == false &&
            l.ProviderStatusCode == 429 &&
            l.OutputText == null &&
            l.ErrorMessage != null), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenUnknownTranslator_ThrowsUnsupportedTranslatorException_WithoutCallingProvider()
    {
        // Arrange
        var request = new TranslateRequest { Text = "hello", Translator = "unknown" };

        // Act
        var act = async () => await _sut.TranslateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnsupportedTranslatorException>();

        _providerMock.Verify(p => p.TranslateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<TranslationLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}