using System.Net.Http.Json;
using System.Text.Json;
using AUIR_Software.Models.LLM;
using AUIR_Software.Services;
using System.Diagnostics;

public sealed class GeminiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiConfig _config;
    private readonly AppConfig _appConfig;
    private readonly ApiRequestPacer _requestPacer;
    private readonly ILogger<GeminiApiClient> _logger;

    public GeminiApiClient(
        HttpClient httpClient,
        AppConfig config,
        ApiRequestPacer requestPacer,
        ILogger<GeminiApiClient> logger)
    {
        _httpClient = httpClient;
        _config = config.Gemini;
        _appConfig = config;
        _requestPacer = requestPacer;
        _logger = logger;
    }

    public async Task<JsonDocument> PostAsync(
        string operation,
        object requestBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            throw new InvalidOperationException(
                "GEMINI_KEY is required when the Gemini provider is selected.");
        }

        var uri = $"{_config.BaseUrl.TrimEnd('/')}/{_config.ApiVersion.Trim('/')}/{operation}";
        var timer = Stopwatch.StartNew();
        _logger.LogInformation("Gemini request started. Operation={Operation}", operation);
        using var response = await _requestPacer.SendAsync(
            "Gemini",
            _appConfig.Rag.GeminiThrottleDelayMs,
            async ct =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = JsonContent.Create(requestBody)
                };
                request.Headers.TryAddWithoutValidation("x-goog-api-key", _config.ApiKey);
                return await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);
            },
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Gemini request failed. Operation={Operation} StatusCode={StatusCode} ElapsedMs={ElapsedMs} Response={Response}",
                operation, (int)response.StatusCode, timer.ElapsedMilliseconds, error);
            throw new HttpRequestException(
                $"Gemini API returned {(int)response.StatusCode} ({response.StatusCode}): {error}",
                null,
                response.StatusCode);
        }

        _logger.LogInformation(
            "Gemini request completed. Operation={Operation} StatusCode={StatusCode} ElapsedMs={ElapsedMs}",
            operation, (int)response.StatusCode, timer.ElapsedMilliseconds);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(
            responseStream,
            cancellationToken: cancellationToken);
    }
}
