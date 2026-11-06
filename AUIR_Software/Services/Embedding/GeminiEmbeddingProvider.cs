using AUIR_Software.Models.LLM;
using System.Text.Json;

namespace AUIR_Software.Services.Embedding
{
    public sealed class GeminiEmbeddingProvider : IEmbeddingProvider
    {
        private readonly GeminiApiClient _api;
        private readonly string _model;
        private int? _dimension;

        public GeminiEmbeddingProvider(GeminiApiClient api, AppConfig config)
        {
            _api = api;
            _model = config.Gemini.EmbeddingModel;
        }

        public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        {
            return EmbedQueryAsync(text, ct);
        }

        public Task<float[]> EmbedDocumentAsync(
            string text,
            string? title,
            CancellationToken ct = default)
        {
            return GenerateEmbeddingAsync(FormatDocument(text, title), ct);
        }

        public async Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
            IReadOnlyList<(string Text, string? Title)> documents,
            CancellationToken ct = default)
        {
            if (documents.Count == 1)
            {
                var document = documents[0];
                return [await EmbedDocumentAsync(document.Text, document.Title, ct)];
            }

            var embeddings = new List<float[]>(documents.Count);
            foreach (var batch in documents.Chunk(100))
            {
                var requests = batch.Select(document => new
                {
                    model = $"models/{_model}",
                    content = new
                    {
                        parts = new[] { new { text = FormatDocument(document.Text, document.Title) } }
                    }
                }).ToArray();

                using var response = await _api.PostAsync(
                    $"models/{Uri.EscapeDataString(_model)}:batchEmbedContents",
                    new { requests },
                    ct);

                var batchEmbeddings = response.RootElement
                    .GetProperty("embeddings")
                    .EnumerateArray()
                    .Select(embedding => embedding
                        .GetProperty("values")
                        .EnumerateArray()
                        .Select(value => value.GetSingle())
                        .ToArray())
                    .ToArray();

                if (batchEmbeddings.Length != batch.Length)
                {
                    throw new InvalidOperationException(
                        $"Gemini returned {batchEmbeddings.Length} embeddings for {batch.Length} documents.");
                }

                foreach (var vector in batchEmbeddings)
                {
                    ValidateDimension(vector);
                    embeddings.Add(vector);
                }
            }

            return embeddings;
        }

        public Task<float[]> EmbedQueryAsync(string text, CancellationToken ct = default)
        {
            return GenerateEmbeddingAsync($"task: question answering | query: {text}", ct);
        }

        public async Task<int> GetDimAsync(CancellationToken ct = default)
        {
            if (_dimension.HasValue)
            {
                return _dimension.Value;
            }

            await EmbedQueryAsync("dimension probe", ct);
            return _dimension!.Value;
        }

        private async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct)
        {
            using var response = await _api.PostAsync(
                $"models/{Uri.EscapeDataString(_model)}:embedContent",
                new { content = new { parts = new[] { new { text } } } },
                ct);

            var values = response.RootElement
                .GetProperty("embedding")
                .GetProperty("values")
                .EnumerateArray()
                .Select(value => value.GetSingle())
                .ToArray();

            ValidateDimension(values);
            return values;
        }

        private static string FormatDocument(string text, string? title) =>
            $"title: {title ?? "none"} | text: {text}";

        private void ValidateDimension(float[] values)
        {
            if (values.Length == 0)
            {
                throw new InvalidOperationException("Gemini returned an empty embedding.");
            }

            if (_dimension.HasValue && _dimension.Value != values.Length)
            {
                throw new InvalidOperationException(
                    $"Gemini embedding dimension changed from {_dimension.Value} to {values.Length}.");
            }

            _dimension ??= values.Length;
        }
    }
}
