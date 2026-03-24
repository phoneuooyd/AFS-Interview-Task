using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Exceptions;

namespace AFS_Interview_Task.Providers.FunTranslations;

public class FunTranslationsProvider : ITranslatorProvider
{
    private readonly HttpClient _httpClient;

    // Use a specific translator type like "leetspeak" for the endpoint
    // In FunTranslations, the endpoint is usually /{translator}.json
    public string TranslatorName => "leetspeak"; 

    public FunTranslationsProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> TranslateAsync(string text, CancellationToken ct)
    {
        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("text", text)
            });

            var response = await _httpClient.PostAsync($"{TranslatorName}.json", content, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(10);
                throw new RateLimitException(retryAfter);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new TranslationProviderException((int)response.StatusCode, $"Translation provider returned status code: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<FunTranslationsResponse>(cancellationToken: ct);

            if (result?.Contents?.Translated == null)
            {
                throw new TranslationProviderException(200, "Translation response was empty or malformed.");
            }

            return result.Contents.Translated;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TranslationTimeoutException();
        }
    }
}

public class FunTranslationsResponse
{
    [JsonPropertyName("success")]
    public SuccessInfo? Success { get; set; }

    [JsonPropertyName("contents")]
    public ContentsInfo? Contents { get; set; }
}

public class SuccessInfo
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class ContentsInfo
{
    [JsonPropertyName("translated")]
    public string? Translated { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("translation")]
    public string? Translation { get; set; }
}