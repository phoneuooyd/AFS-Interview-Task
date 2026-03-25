using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Controllers;
using AFS_Interview_Task.DTOs;
using AFS_Interview_Task.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AFS_Interview_Task.Tests.ControllersTests
{
    public class TranslationControllerTests
    {
        [Fact]
        public async Task Translate_Returns_BadRequest_When_ModelState_Invalid()
        {
            var mockService = new Mock<ITranslationService>();
            var controller = new TranslationController(mockService.Object);
            controller.ModelState.AddModelError("Text", "Required");

            var result = await controller.Translate(new TranslateRequest(), CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }
        [Fact]
        public async Task GetLogs_Enforces_PageSize_Limit()
        {
            var mockService = new Mock<ITranslationService>();
            mockService.Setup(s => s.GetLogsAsync(It.IsAny<TranslationLogQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<TranslationLogDto>(0, 1, 10, new System.Collections.Generic.List<TranslationLogDto>()));

            var controller = new TranslationController(mockService.Object);

            var query = new TranslationLogQuery(Page: 1, PageSize: 1000);
            var result = await controller.GetLogs(query, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);
            var returned = Assert.IsType<PagedResult<TranslationLogDto>>(result.Value);
            Assert.True(returned.PageSize <= 100);
        }

        [Fact]
        public async Task GetLogs_ReturnsBadRequest_WhenFromUtcIsLaterThanToUtc()
        {
            var mockService = new Mock<ITranslationService>();
            var controller = new TranslationController(mockService.Object);

            var query = new TranslationLogQuery(
                Page: 1,
                PageSize: 10,
                FromUtc: DateTime.UtcNow,
                ToUtc: DateTime.UtcNow.AddMinutes(-1));

            var result = await controller.GetLogs(query, CancellationToken.None);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
