using AUIR_Software.Services.IServices;
using AUIR_Software.Services.Embedding;
using AUIR_Software.Services.Vector;
using AUIR_Software.Models.LLM;
using AUIR_Software.Models.DTO;
namespace AUIR_Software.Services
{
    public class RagPipeline
    {
        private readonly ILLMchatProvider _provider;
        private readonly IEmbeddingProvider _emb;
        private readonly IQdrantClient _vec;
        private readonly AppConfig _cfg;

        public RagPipeline(ILLMProviderFactory factory, IEmbeddingProvider emb, IQdrantClient vec, AppConfig cfg)
        {
            _provider = factory.GetProvider();
            _vec = vec;
            _emb = emb;
            _cfg = cfg;
        }

        public async Task IngestAsync(IEnumerable<(string text, string? source)> chunks, CancellationToken ct = default)
        { 
            int dim = await _emb.GetDimAsync(ct);
            await _vec.EnsureCollectionAsync(dim, ct);
            var list = new List<VecPoint>();
            foreach (var (text, source) in chunks)
            {
                var vec = await _emb.EmbedAsync(text, ct);
                list.Add(new VecPoint(Guid.NewGuid().ToString("N"), vec, text, source));
            }
        }

        public async Task<QueryResponse> AskAsync(
            string question,
            int? topK = 5,
            CancellationToken ct = default)
        {
            var dim = await _emb.GetDimAsync(ct);
            await _vec.EnsureCollectionAsync(dim, ct);
            var qvec = await _emb.EmbedAsync(question, ct);
            var hits = await _vec.SearchAsync(qvec, topK ?? _cfg.Rag.TopK, ct);
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
                if(!seenhHits.Contains(fp)) continue;
                seenhHits.Add(fp);
                dedupHits.Add(hit);
            }
            //Make number list of context hits
            var numberedHits = dedupHits.Select((h, i) => (Idx: i +1, Hit: h)).ToList();
            //citation like source list
            var citations = string.Join("\n", numberedHits.Select(x => $"[#${x.Idx}] {x.Hit.Source ?? x.Hit.Id} (score={x.Hit.Score:0.000})"));

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