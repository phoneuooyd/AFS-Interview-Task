using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Domain;
using AFS_Interview_Task.DTOs;
using AFS_Interview_Task.Infrastructure;
using AFS_Interview_Task.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AFS_Interview_Task.Tests.Repositories;

public class TranslationLogRepositoryTests
{
    private readonly AppDbContext _dbContext;
    private readonly TranslationLogRepository _sut;

    public TranslationLogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _sut = new TranslationLogRepository(_dbContext);
    }

    [Fact]
    public async Task QueryAsync_FilterByIsSuccess_ReturnsOnlyMatchingRecords()
    {
        // Arrange
        await SeedLogsAsync();
        var query = new TranslationLogQuery { IsSuccess = true };

        // Act
        var result = await _sut.QueryAsync(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.All(i => i.IsSuccess).Should().BeTrue();
    }

    [Fact]
    public async Task QueryAsync_Pagination_ReturnsCorrectPage_AndTotalCount()
    {
        // Arrange
        await SeedLogsAsync();
        var query = new TranslationLogQuery { Page = 2, PageSize = 2 };

        // Act
        var result = await _sut.QueryAsync(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryAsync_FreeTextSearch_FiltersByInputTextContains()
    {
        // Arrange
        await SeedLogsAsync();
        var query = new TranslationLogQuery { SearchText = "apple" };

        // Act
        var result = await _sut.QueryAsync(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().InputText.Should().Contain("apple");
    }

    private async Task SeedLogsAsync()
    {
        _dbContext.TranslationLogs.AddRange(
            new TranslationLog { Translator = "leetspeak", InputText = "hello", OutputText = "h3ll0", IsSuccess = true, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5) },
            new TranslationLog { Translator = "leetspeak", InputText = "world", OutputText = null, IsSuccess = false, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-4) },
            new TranslationLog { Translator = "yoda", InputText = "apple tree", OutputText = "tree apple", IsSuccess = true, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3) }
        );
        await _dbContext.SaveChangesAsync();
    }
}