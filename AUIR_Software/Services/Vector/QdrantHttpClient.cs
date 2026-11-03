using AUIR_Software.Models.LLM;

namespace AUIR_Software.Services.Vector
{
    public class QdrantHttpClient : IQdrantClient
    {
        private readonly AppConfig _config;
        private readonly HttpClient _http;
        private string? _activeCollection;
        public QdrantHttpClient(IHttpClientFactory f, AppConfig config)
    {
        _config = config;
        _http = f.CreateClient(nameof(QdrantHttpClient));
        _http.BaseAddress = new Uri(config.Qdrant.Endpoint.TrimEnd('/') + '/');
    }

        public async Task EnsureCollectionAsync(int vectorSize, CancellationToken ct = default)
        { 
        var baseName = _config.Qdrant.Collection;
        var name = $"{baseName}_{vectorSize}";
            _activeCollection = name;
            var check = await _http.GetAsync($"collections/{name}", ct);
            if (check.IsSuccessStatusCode) return;
            var body = new
            {
                vectors = new
                {
                    size = vectorSize,
                    distance = "Cosine"
                }
            };
            var response = await _http.PutAsJsonAsync($"collections/{name}", body, ct);
            response.EnsureSuccessStatusCode();

        }

        public async Task UpsertAsync(IEnumerable<VecPoint> points, CancellationToken ct = default)
        {
            var name = _activeCollection ?? _config.Qdrant.Collection;
            var payload = new
            {
                points = points.Select(p => new
                {
                    id = p.Id,
                    vector = p.Vector,
                    payload = new
                    {
                        text = p.Text,
                        source = p.Source
                    }
                })
            };
            var response = await _http.PostAsJsonAsync($"collections/{name}/points", payload, ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<VecHit>> SearchAsync(float[] query, int topK, CancellationToken ct = default)
        {
            var name = _activeCollection ?? _config.Qdrant.Collection;
            var payload = new
            {
                vector = query,
                limit = topK,
                with_payload = true
            };
            var response = await _http.PostAsJsonAsync($"collections/{name}/points/search", payload, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(cancellationToken: ct);
            var results = new List<VecHit>();
            foreach (var r in json.Result)
            {
                results.Add(new VecHit(r.Id, r.Score, r.Payload.Text, r.Payload.Source));
            }
            return results;
        }
    }
}






