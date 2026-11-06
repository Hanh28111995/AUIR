namespace AUIR_Software.Services.Embedding
{
    public interface IEmbeddingProvider
    {
        Task<float[]>EmbedAsync(string text, CancellationToken ct = default);        
        Task<int> GetDimAsync(CancellationToken ct = default);

        Task<float[]> EmbedDocumentAsync(
            string text,
            string? title,
            CancellationToken ct = default) => EmbedAsync(text, ct);

        async Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
            IReadOnlyList<(string Text, string? Title)> documents,
            CancellationToken ct = default)
        {
            var embeddings = new List<float[]>(documents.Count);
            foreach (var (text, title) in documents)
            {
                embeddings.Add(await EmbedDocumentAsync(text, title, ct));
            }

            return embeddings;
        }

        Task<float[]> EmbedQueryAsync(
            string text,
            CancellationToken ct = default) => EmbedAsync(text, ct);
    }
}
