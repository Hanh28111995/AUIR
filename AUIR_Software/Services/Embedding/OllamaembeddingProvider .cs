using System.Text;
using System.Text.Json;

namespace AUIR_Software.Services.Embedding
{
    public class OllamaEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        private int? _dim;

        public OllamaEmbeddingProvider(
            HttpClient httpClient,
            IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<float[]> EmbedAsync(
            string text,
            CancellationToken ct = default)
        {
            var baseUrl = _config["Ollama:BaseUrl"];
            var model = _config["Ollama:EmbeddingModel"];

            var body = new
            {
                model = model,
                prompt = text
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{baseUrl}/api/embeddings",
                content,
                ct);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(json);

            var embedding = doc.RootElement
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

            _dim ??= embedding.Length;

            return embedding;
        }

        public async Task<int> GetDimAsync(
            CancellationToken ct = default)
        {
            if (_dim.HasValue)
            {
                return _dim.Value;
            }

            await EmbedAsync("dimension probe", ct);

            return _dim!.Value;
        }
    }
}