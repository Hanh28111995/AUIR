using AUIR_Software.Services.IServices;
using AUIR_Software.Services.Embedding;
using AUIR_Software.Services.Vector;
using AUIR_Software.Models.LLM;
using AUIR_Software.Models.DTO;
using System.Diagnostics;
namespace AUIR_Software.Services
{
    public class RagPipeline
    {
        private readonly ILLMchatProvider _provider;
        private readonly IEmbeddingProvider _emb;
        private readonly IQdrantClient _vec;
        private readonly AppConfig _cfg;
        private readonly ILogger<RagPipeline> _logger;

        public RagPipeline(
            ILLMProviderFactory factory,
            IEmbeddingProvider emb,
            IQdrantClient vec,
            AppConfig cfg,
            ILogger<RagPipeline> logger)
        {
            _provider = factory.GetProvider();
            _vec = vec;
            _emb = emb;
            _cfg = cfg;
            _logger = logger;
        }

        public async Task IngestAsync(IEnumerable<(string text, string? source)> chunks, CancellationToken ct = default)
        {
            var documents = chunks.ToList();
            if (documents.Count == 0) return;

            int dim = await _emb.GetDimAsync(ct);
            await _vec.EnsureCollectionAsync(dim, ct);
            var embeddings = await _emb.EmbedDocumentsAsync(
                documents.Select(chunk => (Text: chunk.text, Title: chunk.source)).ToList(),
                ct);
            if (embeddings.Count != documents.Count)
            {
                throw new InvalidOperationException(
                    $"Embedding provider returned {embeddings.Count} embeddings for {documents.Count} chunks.");
            }

            var points = documents
                .Select((chunk, index) => new VecPoint(
                    Guid.NewGuid().ToString(),
                    embeddings[index],
                    chunk.text,
                    chunk.source))
                .ToList();
            await _vec.UpsertAsync(points, ct);
        }

        public async Task ReplaceDocumentAsync(string source, IEnumerable<string> chunks, CancellationToken ct = default)
        {
            var texts = chunks.ToList();
            if (texts.Count == 0) return;

            var dimensionTimer = Stopwatch.StartNew();
            int dim;
            try
            {
                dim = await _emb.GetDimAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RAG ingestion failed while requesting embedding dimensions. Source={Source} ChunkCount={ChunkCount} ElapsedMs={ElapsedMs}",
                    source,
                    texts.Count,
                    dimensionTimer.ElapsedMilliseconds);
                throw;
            }
            _logger.LogInformation(
                "Embedding dimensions resolved. Source={Source} Dimension={Dimension} ElapsedMs={ElapsedMs}",
                source,
                dim,
                dimensionTimer.ElapsedMilliseconds);

            var collectionTimer = Stopwatch.StartNew();
            try
            {
                await _vec.EnsureCollectionAsync(dim, ct);
                _logger.LogInformation(
                    "Qdrant collection checked. Source={Source} ElapsedMs={ElapsedMs}",
                    source,
                    collectionTimer.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RAG ingestion failed while checking Qdrant collection. Source={Source} ElapsedMs={ElapsedMs}",
                    source,
                    collectionTimer.ElapsedMilliseconds);
                throw;
            }

            var points = new List<VecPoint>(texts.Count);
            var embeddingTimer = Stopwatch.StartNew();
            IReadOnlyList<float[]> embeddings;
            try
            {
                embeddings = await _emb.EmbedDocumentsAsync(
                    texts.Select(text => (Text: text, Title: (string?)source)).ToList(),
                    ct);
                _logger.LogInformation(
                    "RAG embeddings created. Source={Source} ChunkCount={ChunkCount} ElapsedMs={ElapsedMs}",
                    source,
                    texts.Count,
                    embeddingTimer.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RAG ingestion failed during embedding. Source={Source} ChunkCount={ChunkCount} ElapsedMs={ElapsedMs}",
                    source,
                    texts.Count,
                    embeddingTimer.ElapsedMilliseconds);
                throw;
            }

            for (var index = 0; index < texts.Count; index++)
            {
                points.Add(new VecPoint(
                    Guid.NewGuid().ToString(),
                    embeddings[index],
                    texts[index],
                    source));
            }

            var deleteTimer = Stopwatch.StartNew();
            try
            {
                await _vec.DeleteBySourceAsync(source, ct);
                _logger.LogInformation(
                    "RAG previous document points deleted. Source={Source} ElapsedMs={ElapsedMs}",
                    source,
                    deleteTimer.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RAG ingestion failed while deleting previous Qdrant points. Source={Source} ElapsedMs={ElapsedMs}",
                    source,
                    deleteTimer.ElapsedMilliseconds);
                throw;
            }

            var upsertTimer = Stopwatch.StartNew();
            try
            {
                await _vec.UpsertAsync(points, ct);
                _logger.LogInformation(
                    "RAG points upserted to Qdrant. Source={Source} PointCount={PointCount} ElapsedMs={ElapsedMs}",
                    source,
                    points.Count,
                    upsertTimer.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RAG ingestion failed while upserting points to Qdrant. Source={Source} PointCount={PointCount} ElapsedMs={ElapsedMs}",
                    source,
                    points.Count,
                    upsertTimer.ElapsedMilliseconds);
                throw;
            }
        }

        public Task<List<RagDocument>> ListDocumentsAsync(CancellationToken ct = default) =>
            _vec.ListDocumentsAsync(ct);

        public Task DeleteDocumentAsync(string source, CancellationToken ct = default) =>
            _vec.DeleteBySourceAsync(source, ct);

        public async Task<QueryResponse> AskAsync(
            string question,
            int? topK = 5,
            CancellationToken ct = default)
        {
            var dim = await _emb.GetDimAsync(ct);
            await _vec.EnsureCollectionAsync(dim, ct);
            var qvec = await _emb.EmbedQueryAsync(question, ct);
            var hits = await _vec.SearchAsync(qvec, topK ?? _cfg.Rag.TopK, ct: ct);
            //filter hits by score
            var filterHits = hits.Where(h => h.Score >= _cfg.Rag.MinScore).ToList();
            if(filterHits.Count == 0)
            {
                return new QueryResponse("Can't find any matchedd context data in your knowledge base.", new List<QueryHit>());

            }
            //duplicate context data'
            var seenhHits = new HashSet<string>();
            var dedupHits = new List<VecHit>();
            foreach (var hit in filterHits)
            {
                var fp = FingerPrint(hit.Text);
                if (seenhHits.Contains(fp)) continue;
                seenhHits.Add(fp);
                dedupHits.Add(hit);
            }
            //Make number list of context hits
            var numberedHits = dedupHits.Select((h, i) => (Idx: i +1, Hit: h)).ToList();
            //citation like source list
            var citations = string.Join("\n", numberedHits.Select(x => $"[#{x.Idx}] {x.Hit.Source ?? x.Hit.Id} (score={x.Hit.Score:0.000})"));

            var context = string.Join("\n---\n", numberedHits.Select(x => $"ID:{x.Idx}\n{x.Hit.Text}"));
            string relevantDocs = string.Empty;

            var systemPrompt = $"You must answer ONLY using the CONTEXT. If not found, reply:'I dont know'. " +
                "Cite context IDs in square brackets like [#1], [#2]. Be concise.";                            

            var userPrompt = $"Question: \n{question}\n\nAnswer: \n{context}";

            var answer = await _provider.AskAsync(systemPrompt, userPrompt, ct);
            answer += $"\n\n Sources:\n {citations}";

            var responseHits = dedupHits.Select(h => new QueryHit
            (
                h.Id,
                h.Score,
                h.Text,
                h.Source
            )).ToList();

            return new QueryResponse(answer, responseHits );
        }
        static string FingerPrint(string s) {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(s.Trim().ToLowerInvariant());
            return Convert.ToBase64String(sha256.ComputeHash(bytes)).Substring(0, 16);
        }
    }
}