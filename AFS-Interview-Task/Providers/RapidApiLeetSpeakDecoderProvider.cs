using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AFS_Interview_Task.Exceptions;
using Microsoft.Extensions.Options;

namespace AFS_Interview_Task.Providers;

public class RapidApiLeetSpeakDecoderProvider : ITranslatorProvider
{
    private const string SupportedTranslator = "leetspeak";

    private readonly HttpClient _httpClient;
    private readonly RapidApiLeetDecoderOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ProviderKey => "rapidapi";

    public RapidApiLeetSpeakDecoderProvider(HttpClient httpClient, IOptions<RapidApiLeetDecoderOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("RapidAPI Leet decoder API key is not configured.");
        }
    }

    public Task<string> TranslateAsync(string text, CancellationToken ct)
        => SendDecodeRequestAsync(text, ct);

    public Task<string> TranslateAsync(string translator, string text, CancellationToken ct)
    {
        if (!string.Equals(translator, SupportedTranslator, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnsupportedTranslatorException(translator);
        }

        return SendDecodeRequestAsync(text, ct);
    }

    private async Task<string> SendDecodeRequestAsync(string text, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.EndpointPath)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { text, mode = "decode" }, _jsonOptions),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("x-rapidapi-host", _options.Host);
        request.Headers.Add("x-rapidapi-key", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new TranslationProviderException((int)response.StatusCode, $"RapidAPI leet decoder returned {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        return ExtractDecodedText(body) ?? body;
    }

    private static string? ExtractDecodedText(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement
                .GetProperty("data")
                .GetProperty("text")
                .GetString();
        }
        catch
        {
            return null;
        }
    }
}
