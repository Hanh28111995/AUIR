
using OpenAI.Embeddings;
namespace AUIR_Software.Services.Embedding
{
    public class OpenAIembeddingProvider : IEmbeddingProvider
    {
        private readonly EmbeddingClient _client;
        private int? _dim;

        public OpenAIembeddingProvider(EmbeddingClient client)
        {
            _client = client;
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        {
            var response = await _client.GenerateEmbeddingAsync(
                text,
                cancellationToken: ct);
            var vec = response.Value.ToFloats().ToArray();
            _dim ??= vec.Length;
             return vec;            
        }
        public async Task<int> GetDimAsync(CancellationToken ct = default)
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

