using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Controllers;
using AFS_Interview_Task.DTOs;
using AFS_Interview_Task.Providers;
using AFS_Interview_Task.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AFS_Interview_Task.Tests.ControllersTests;

public class TranslationControllerTests
{
    [Fact]
    public async Task Translate_Returns_BadRequest_When_ModelState_Invalid()
    {
        var mockService = new Mock<ITranslationService>();
        var controller = CreateController(mockService, defaultProvider: "rapidapi");
        controller.ModelState.AddModelError("Text", "Required");

        var result = await controller.Translate(new TranslateRequest(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Translate_Returns_BadRequest_When_FunTranslations_And_Translator_Missing()
    {
        var mockService = new Mock<ITranslationService>();
        var controller = CreateController(mockService, defaultProvider: "funtranslations");

        var result = await controller.Translate(new TranslateRequest { Text = "hello" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetLogs_Enforces_PageSize_Limit()
    {
        var mockService = new Mock<ITranslationService>();
        mockService.Setup(s => s.GetLogsAsync(It.IsAny<TranslationLogQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<TranslationLogDto>(0, 1, 10, new System.Collections.Generic.List<TranslationLogDto>()));

        var controller = CreateController(mockService, defaultProvider: "rapidapi");

        var query = new TranslationLogQuery(Page: 1, PageSize: 1000);
        var result = await controller.GetLogs(query, CancellationToken.None) as OkObjectResult;

        Assert.NotNull(result);
        var returned = Assert.IsType<PagedResult<TranslationLogDto>>(result.Value);
        Assert.True(returned.PageSize <= 100);
    }

    private static TranslationController CreateController(Mock<ITranslationService> service, string defaultProvider)
    {
        var options = Options.Create(new TranslationExecutionOptions { DefaultProvider = defaultProvider });
        return new TranslationController(service.Object, options);
    }
}
