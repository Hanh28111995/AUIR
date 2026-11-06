using AUIR_Software.Models.LLM;
using AUIR_Software.Services;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AUIR_Software.Services.Vector
{
    public class QdrantHttpClient : IQdrantClient
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);
        private readonly AppConfig _config;
        private readonly HttpClient _http;
        private readonly ApiRequestPacer _requestPacer;
        private string? _activeCollection;
        public QdrantHttpClient(
            IHttpClientFactory f,
            AppConfig config,
            ApiRequestPacer requestPacer)
        {
            if (!Uri.TryCreate(config.Qdrant.Endpoint, UriKind.Absolute, out var endpoint) ||
                endpoint.Scheme is not ("http" or "https"))
            {
                throw new InvalidOperationException(
                    "Qdrant:Endpoint must be an absolute HTTP or HTTPS URL.");
            }

            var isLocalEndpoint = endpoint.IsLoopback ||
                string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase);
            if (!isLocalEndpoint && string.IsNullOrWhiteSpace(config.Qdrant.ApiKey))
            {
                throw new InvalidOperationException(
                    "Qdrant:ApiKey is required for non-local Qdrant endpoints.");
            }

            if (config.Rag.QdrantRequestTimeoutSeconds is < 1 or > 600)
            {
                throw new InvalidOperationException(
                    "Rag:QdrantRequestTimeoutSeconds must be between 1 and 600.");
            }

            _config = config;
            _requestPacer = requestPacer;
            _http = f.CreateClient(nameof(QdrantHttpClient));
            _http.BaseAddress = new Uri(endpoint.AbsoluteUri.TrimEnd('/') + '/');
            _http.Timeout = TimeSpan.FromSeconds(config.Rag.QdrantRequestTimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(config.Qdrant.ApiKey))
            {
                _http.DefaultRequestHeaders.Add("api-key", config.Qdrant.ApiKey);
            }
        }

        public async Task EnsureCollectionAsync(int vectorSize, CancellationToken ct = default)
        { 
        var baseName = _config.Qdrant.Collection;
        var name = $"{baseName}_{vectorSize}";
            _activeCollection = name;
            using var check = await SendAsync(
                token => _http.GetAsync($"collections/{name}", token), ct);
            if (!check.IsSuccessStatusCode)
            {
                if (check.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    await EnsureSuccessAsync(check, $"GET collections/{name}", name, ct);
                }

                var body = new
                {
                    vectors = new
                    {
                        size = vectorSize,
                        distance = "Cosine"
                    }
                };
                using var response = await SendAsync(
                    token => _http.PutAsJsonAsync($"collections/{name}", body, token), ct);
                await EnsureSuccessAsync(response, $"PUT collections/{name}", name, ct);
            }

            await EnsurePayloadIndexAsync(name, ct);
        }

        private async Task EnsurePayloadIndexAsync(string collection, CancellationToken ct)
        {
            var body = new
            {
                field_name = "source",
                field_schema = "keyword"
            };
            using var response = await SendAsync(
                token => _http.PutAsJsonAsync(
                    $"collections/{collection}/index?wait=true", body, token), ct);
            await EnsureSuccessAsync(
                response,
                $"PUT collections/{collection}/index?wait=true (source/keyword)",
                collection,
                ct);
        }

        public async Task UpsertAsync(IEnumerable<VecPoint> points, CancellationToken ct = default)
        {
            var name = _activeCollection ?? _config.Qdrant.Collection;
            var batchSize = _config.Rag.QdrantUpsertBatchSize;
            if (batchSize is < 1 or > 100)
            {
                throw new InvalidOperationException(
                    "Rag:QdrantUpsertBatchSize must be between 1 and 100.");
            }
            var maxRequestBytes = _config.Rag.QdrantMaxRequestBytes;
            if (maxRequestBytes is < 1024 or > 64 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "Rag:QdrantMaxRequestBytes must be between 1024 and 67108864.");
            }

            var batch = new List<QdrantPoint>(batchSize);
            foreach (var point in points)
            {
                var candidate = batch.Append(new QdrantPoint(
                    point.Id,
                    point.Vector,
                    new QdrantPointPayload(point.Text, point.Source))).ToList();
                var candidateBody = JsonSerializer.SerializeToUtf8Bytes(
                    new QdrantUpsertRequest(candidate), JsonOptions);
                if (candidateBody.Length > maxRequestBytes)
                {
                    if (batch.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"A single Qdrant point is larger than Rag:QdrantMaxRequestBytes ({maxRequestBytes} bytes).");
                    }

                    await UpsertBatchAsync(name, batch, ct);
                    batch.Clear();
                    candidate = [new QdrantPoint(
                        point.Id,
                        point.Vector,
                        new QdrantPointPayload(point.Text, point.Source))];
                    candidateBody = JsonSerializer.SerializeToUtf8Bytes(
                        new QdrantUpsertRequest(candidate), JsonOptions);
                    if (candidateBody.Length > maxRequestBytes)
                    {
                        throw new InvalidOperationException(
                            $"A single Qdrant point is larger than Rag:QdrantMaxRequestBytes ({maxRequestBytes} bytes).");
                    }
                }

                batch.Add(candidate[^1]);
                if (batch.Count >= batchSize)
                {
                    await UpsertBatchAsync(name, batch, ct);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await UpsertBatchAsync(name, batch, ct);
            }
        }

        private async Task UpsertBatchAsync(
            string collection,
            IReadOnlyList<QdrantPoint> points,
            CancellationToken ct)
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(
                new QdrantUpsertRequest(points), JsonOptions);
            using var response = await SendAsync(token =>
            {
                var content = new ByteArrayContent(body);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return _http.PutAsync(
                    $"collections/{collection}/points?wait=true", content, token);
            }, ct);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"Qdrant upsert failed with HTTP {(int)response.StatusCode} ({response.StatusCode}); " +
                    $"requestBytes={body.Length}; response={responseBody}",
                    null,
                    response.StatusCode);
            }
        }

        private sealed record QdrantUpsertRequest(IReadOnlyList<QdrantPoint> Points);
        private sealed record QdrantPoint(string Id, float[] Vector, QdrantPointPayload Payload);
        private sealed record QdrantPointPayload(string Text, string? Source);

        public async Task<List<VecHit>> SearchAsync(
            float[] query,
            int topK,
            string? source = null,
            CancellationToken ct = default)
        {
            var name = _activeCollection ?? _config.Qdrant.Collection;
            var payload = new
            {
                vector = query,
                limit = topK,
                with_payload = true,
                filter = string.IsNullOrWhiteSpace(source)
                    ? null
                    : new
                    {
                        must = new[]
                        {
                            new
                            {
                                key = "source",
                                match = new { value = source }
                            }
                        }
                    }
            };
            using var response = await SendAsync(
                token => _http.PostAsJsonAsync($"collections/{name}/points/search", payload, token), ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(cancellationToken: ct);
            var results = new List<VecHit>();
            foreach (var r in json.Result)
            {
                results.Add(new VecHit(r.Id, r.Score, r.Payload.Text, r.Payload.Source));
            }
            return results;
        }

        public async Task<List<RagDocument>> ListDocumentsAsync(CancellationToken ct = default)
        {
            var collections = await GetRagCollectionsAsync(ct);
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in collections)
            {
                JsonElement? offset = null;
                while (true)
                {
                    var payload = new
                    {
                        limit = 256,
                        with_payload = new[] { "source" },
                        with_vector = false,
                        offset
                    };
                    using var response = await SendAsync(
                        token => _http.PostAsJsonAsync(
                            $"collections/{name}/points/scroll", payload, token), ct);
                    response.EnsureSuccessStatusCode();
                    var page = await response.Content.ReadFromJsonAsync<QdrantScrollResponse>(
                            cancellationToken: ct)
                        ?? throw new InvalidOperationException("Qdrant returned an empty scroll response.");

                    foreach (var point in page.Result?.Points ?? [])
                    {
                        if (point.Payload is null ||
                            !point.Payload.TryGetValue("source", out var sourceValue) ||
                            sourceValue.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var source = sourceValue.GetString();
                        if (!string.IsNullOrWhiteSpace(source))
                        {
                            counts[source] = counts.GetValueOrDefault(source) + 1;
                        }
                    }

                    var nextOffset = page.Result?.NextPageOffset;
                    if (!nextOffset.HasValue ||
                        nextOffset.Value.ValueKind == JsonValueKind.Null ||
                        (offset.HasValue && offset.Value.GetRawText() == nextOffset.Value.GetRawText()))
                    {
                        break;
                    }

                    offset = nextOffset;
                }
            }

            return counts
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => new RagDocument(item.Key, item.Value))
                .ToList();
        }

        public async Task DeleteBySourceAsync(string source, CancellationToken ct = default)
        {
            var collections = await GetRagCollectionsAsync(ct);
            var payload = new
            {
                filter = new
                {
                    must = new[]
                    {
                        new
                        {
                            key = "source",
                            match = new { value = source }
                        }
                    }
                }
            };
            foreach (var name in collections)
            {
                using var response = await SendAsync(
                    token => _http.PostAsJsonAsync(
                        $"collections/{name}/points/delete?wait=true", payload, token), ct);
                await EnsureSuccessAsync(
                    response,
                    $"POST collections/{name}/points/delete?wait=true",
                    name,
                    ct);
            }
        }

        private async Task<IReadOnlyList<string>> GetRagCollectionsAsync(CancellationToken ct)
        {
            using var response = await SendAsync(
                token => _http.GetAsync("collections", token), ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<QdrantCollectionsResponse>(
                cancellationToken: ct)
                ?? throw new InvalidOperationException("Qdrant returned an empty collections response.");

            var baseName = _config.Qdrant.Collection;
            var matchingCollections = (result.Result?.Collections ?? [])
                .Select(collection => collection.Name)
                .Where(name =>
                    string.Equals(name, baseName, StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith(baseName + "_", StringComparison.OrdinalIgnoreCase))
                .ToList();

            return matchingCollections;
        }

        private sealed class QdrantCollectionsResponse
        {
            public QdrantCollectionsResult? Result { get; set; }
        }

        private sealed class QdrantCollectionsResult
        {
            public List<QdrantCollection> Collections { get; set; } = [];
        }

        private sealed class QdrantCollection
        {
            public string Name { get; set; } = string.Empty;
        }

        private Task<HttpResponseMessage> SendAsync(
            Func<CancellationToken, Task<HttpResponseMessage>> send,
            CancellationToken ct) =>
            _requestPacer.SendAsync(
                "Qdrant",
                _config.Rag.QdrantThrottleDelayMs,
                send,
                ct);

        private static async Task EnsureSuccessAsync(
            HttpResponseMessage response,
            string operation,
            string collection,
            CancellationToken ct)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Qdrant operation failed. Operation={operation}; " +
                $"Collection={collection}; HTTP={(int)response.StatusCode} ({response.StatusCode}); " +
                $"Response={responseBody}",
                null,
                response.StatusCode);
        }

        private sealed class QdrantScrollResponse
        {
            public QdrantScrollResult? Result { get; set; }
        }

        private sealed class QdrantScrollResult
        {
            public List<QdrantScrollPoint> Points { get; set; } = [];

            [JsonPropertyName("next_page_offset")]
            public JsonElement? NextPageOffset { get; set; }
        }

        private sealed class QdrantScrollPoint
        {
            public Dictionary<string, JsonElement>? Payload { get; set; }
        }
    }
}
