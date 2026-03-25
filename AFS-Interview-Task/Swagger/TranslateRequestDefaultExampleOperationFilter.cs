using AFS_Interview_Task.Providers;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AFS_Interview_Task.Swagger;

public class TranslateRequestDefaultExampleOperationFilter : IOperationFilter
{
    private readonly LeetSpeakTranslationOptions _options;

    public TranslateRequestDefaultExampleOperationFilter(IOptions<LeetSpeakTranslationOptions> options)
    {
        _options = options.Value;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isTranslatePost =
            context.ApiDescription.HttpMethod == "POST" &&
            context.ApiDescription.RelativePath?.Equals("api/translate", System.StringComparison.OrdinalIgnoreCase) == true;

        if (!isTranslatePost || operation.RequestBody?.Content == null)
        {
            return;
        }

        var example = new OpenApiObject
        {
            ["text"] = new OpenApiString("1337"),
            ["translator"] = new OpenApiString("leetspeak")
        };

        foreach (var content in operation.RequestBody.Content.Values)
        {
            content.Example = example;
        }
    }
}
