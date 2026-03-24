namespace AFS_Interview_Task.Providers;

public class RapidApiLeetDecoderOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string EndpointPath { get; set; } = "v1/decode";
    public string Host { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
