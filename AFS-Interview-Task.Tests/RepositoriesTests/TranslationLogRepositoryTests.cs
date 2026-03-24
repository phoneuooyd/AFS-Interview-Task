using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Domain;
using AFS_Interview_Task.DTOs;
using AFS_Interview_Task.Infrastructure;
using AFS_Interview_Task.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AFS_Interview_Task.Tests.RepositoriesTests;

public class TranslationLogRepositoryTests
{
    [Fact]
    public async Task QueryAsync_AppliesFiltersAndPagination()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"translation-log-tests-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new AppDbContext(dbOptions);

        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        dbContext.TranslationLogs.AddRange(
            new TranslationLog
            {
                Translator = "pirate",
                InputText = "hello there",
                OutputText = "ahoy there",
                IsSuccess = true,
                ProviderStatusCode = 200,
                CreatedAtUtc = start.AddDays(1)
            },
            new TranslationLog
            {
                Translator = "pirate",
                InputText = "hello friend",
                OutputText = "ahoy matey",
                IsSuccess = true,
                ProviderStatusCode = 200,
                CreatedAtUtc = start.AddDays(2)
            },
            new TranslationLog
            {
                Translator = "yoda",
                InputText = "hello there",
                OutputText = "there hello",
                IsSuccess = true,
                ProviderStatusCode = 200,
                CreatedAtUtc = start.AddDays(3)
            },
            new TranslationLog
            {
                Translator = "pirate",
                InputText = "different text",
                ErrorMessage = "Rate limited",
                IsSuccess = false,
                ProviderStatusCode = 429,
                CreatedAtUtc = start.AddDays(4)
            });

        await dbContext.SaveChangesAsync();

        var repository = new TranslationLogRepository(dbContext);

        var query = new TranslationLogQuery(
            Page: 1,
            PageSize: 1,
            Translator: "pirate",
            IsSuccess: true,
            FromUtc: start,
            ToUtc: start.AddDays(3),
            SearchText: "hello"
        );

        var result = await repository.QueryAsync(query, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);

        var returned = result.Items.Single();
        Assert.Equal("pirate", returned.Translator);
        Assert.True(returned.IsSuccess);
        Assert.Contains("hello", returned.InputText, StringComparison.OrdinalIgnoreCase);

        var expectedLatestMatching = dbContext.TranslationLogs
            .Where(l => l.Translator == "pirate" && l.IsSuccess && l.InputText.Contains("hello") && l.CreatedAtUtc <= start.AddDays(3))
            .OrderByDescending(l => l.CreatedAtUtc)
            .First();

        Assert.Equal(expectedLatestMatching.Id, returned.Id);
    }

    [Fact]
    public async Task QueryAsync_FilterByIsSuccess_ReturnsOnlyMatchingRecords()
    {
        await using var dbContext = CreateContext();
        await SeedLogsAsync(dbContext);

        var repository = new TranslationLogRepository(dbContext);
        var query = new TranslationLogQuery { IsSuccess = true };

        var result = await repository.QueryAsync(query, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.True(item.IsSuccess));
    }

    [Fact]
    public async Task QueryAsync_SecondPage_ReturnsRemainingItemsAndTotalCount()
    {
        await using var dbContext = CreateContext();
        await SeedLogsAsync(dbContext);

        var repository = new TranslationLogRepository(dbContext);
        var query = new TranslationLogQuery { Page = 2, PageSize = 2 };

        var result = await repository.QueryAsync(query, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task QueryAsync_SearchText_FiltersByInputText()
    {
        await using var dbContext = CreateContext();
        await SeedLogsAsync(dbContext);

        var repository = new TranslationLogRepository(dbContext);
        var query = new TranslationLogQuery { SearchText = "apple" };

        var result = await repository.QueryAsync(query, CancellationToken.None);

        var onlyItem = Assert.Single(result.Items);
        Assert.Contains("apple", onlyItem.InputText, StringComparison.OrdinalIgnoreCase);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"translation-log-repo-tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedLogsAsync(AppDbContext dbContext)
    {
        dbContext.TranslationLogs.AddRange(
            new TranslationLog { Translator = "leetspeak", InputText = "hello", OutputText = "h3ll0", IsSuccess = true, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5) },
            new TranslationLog { Translator = "leetspeak", InputText = "world", OutputText = null, IsSuccess = false, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-4) },
            new TranslationLog { Translator = "yoda", InputText = "apple tree", OutputText = "tree apple", IsSuccess = true, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3) }
        );

        await dbContext.SaveChangesAsync();
    }
}
