using System.Collections.Concurrent;
using System.Diagnostics;
using AUIR_Software.Models.LLM;

namespace AUIR_Software.Services;

public sealed class ApiRequestPacer
{
    private const int MaxRetryDelayMilliseconds = 30_000;
    private readonly AppConfig _config;
    private readonly ILogger<ApiRequestPacer> _logger;
    private readonly ConcurrentDictionary<string, RequestGate> _gates = new();

    public ApiRequestPacer(AppConfig config, ILogger<ApiRequestPacer> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> SendAsync(
        string service,
        int throttleDelayMilliseconds,
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        if (throttleDelayMilliseconds < 0 || throttleDelayMilliseconds > 60_000)
        {
            throw new InvalidOperationException(
                $"Rag:{service}ThrottleDelayMs must be between 0 and 60000.");
        }

        if (_config.Rag.HttpMaxRetries is < 0 or > 10)
        {
            throw new InvalidOperationException("Rag:HttpMaxRetries must be between 0 and 10.");
        }

        if (_config.Rag.HttpRetryBaseDelayMs is < 1 or > MaxRetryDelayMilliseconds)
        {
            throw new InvalidOperationException(
                $"Rag:HttpRetryBaseDelayMs must be between 1 and {MaxRetryDelayMilliseconds}.");
        }

        var gate = _gates.GetOrAdd(service, _ => new RequestGate());
        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await SendPacedAsync(
                    gate, throttleDelayMilliseconds, send, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < _config.Rag.HttpMaxRetries)
            {
                var delay = GetBackoffDelay(attempt + 1);
                _logger.LogWarning(
                    "Transient {Service} network error. Retry={Retry}/{MaxRetries} DelayMs={DelayMs}",
                    service, attempt + 1, _config.Rag.HttpMaxRetries, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
                continue;
            }
            catch (TaskCanceledException) when (
                !cancellationToken.IsCancellationRequested &&
                attempt < _config.Rag.HttpMaxRetries)
            {
                var delay = GetBackoffDelay(attempt + 1);
                _logger.LogWarning(
                    "Timed out calling {Service}. Retry={Retry}/{MaxRetries} DelayMs={DelayMs}",
                    service, attempt + 1, _config.Rag.HttpMaxRetries, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            if (!IsTransient(response.StatusCode) || attempt >= _config.Rag.HttpMaxRetries)
            {
                return response;
            }

            var statusCode = response.StatusCode;
            var retryDelay = GetRetryDelay(response, attempt + 1);
            response.Dispose();
            _logger.LogWarning(
                "{Service} returned a retryable HTTP status. StatusCode={StatusCode} Retry={Retry}/{MaxRetries} DelayMs={DelayMs}",
                service,
                (int)statusCode,
                attempt + 1,
                _config.Rag.HttpMaxRetries,
                retryDelay.TotalMilliseconds);
            await Task.Delay(retryDelay, cancellationToken);
        }
    }

    private static async Task<HttpResponseMessage> SendPacedAsync(
        RequestGate gate,
        int throttleDelayMilliseconds,
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        await gate.Semaphore.WaitAsync(cancellationToken);
        try
        {
            if (gate.LastRequestTimestamp != 0 && throttleDelayMilliseconds > 0)
            {
                var elapsed = Stopwatch.GetElapsedTime(gate.LastRequestTimestamp);
                var remaining = TimeSpan.FromMilliseconds(throttleDelayMilliseconds) - elapsed;
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining, cancellationToken);
                }
            }

            gate.LastRequestTimestamp = Stopwatch.GetTimestamp();
            return await send(cancellationToken);
        }
        finally
        {
            gate.Semaphore.Release();
        }
    }

    private TimeSpan GetRetryDelay(HttpResponseMessage response, int retryNumber)
    {
        var retryAfter = response.Headers.RetryAfter;
        var serverDelay = retryAfter?.Delta ??
            (retryAfter?.Date is { } retryDate
                ? retryDate - DateTimeOffset.UtcNow
                : null);
        if (serverDelay is { } requestedDelay && requestedDelay > TimeSpan.Zero)
        {
            return requestedDelay > TimeSpan.FromMinutes(5)
                ? TimeSpan.FromMinutes(5)
                : requestedDelay;
        }

        return GetBackoffDelay(retryNumber);
    }

    private TimeSpan GetBackoffDelay(int retryNumber)
    {
        var delay = _config.Rag.HttpRetryBaseDelayMs * Math.Pow(2, retryNumber - 1);
        return TimeSpan.FromMilliseconds(Math.Min(delay, MaxRetryDelayMilliseconds));
    }

    private static bool IsTransient(System.Net.HttpStatusCode statusCode) =>
        statusCode is System.Net.HttpStatusCode.RequestTimeout or
            System.Net.HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    private sealed class RequestGate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public long LastRequestTimestamp { get; set; }
    }
}
