namespace AUIR_Software.Models.LLM
{
    public class AppConfig
    {
        public GeminiConfig Gemini { get; set; } = new GeminiConfig();
        public QdrantConfig Qdrant { get; set; } = new QdrantConfig();
        public RagConfig Rag { get; set; } = new RagConfig();



    }

    public class GeminiConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
        public string ApiVersion { get; set; } = "v1beta";
        public string ChatModel { get; set; } = "gemini-flash-latest";
        public string EmbeddingModel { get; set; } = "gemini-embedding-2";
        public int EmbeddingOutputDimensionality { get; set; } = 768;
    }

    public class QdrantConfig
    {
        public string Endpoint { get; set; } = "http://localhost:6333";
        public string ApiKey { get; set; } = string.Empty;
        public string Collection { get; set; } = "auir_collection";
    }

    public class RagConfig
    {
        public int ChunkSize { get; set; } = 880;
        public int ChunkOverlap { get; set; } = 120;
        public int QdrantUpsertBatchSize { get; set; } = 100;
        public int QdrantMaxRequestBytes { get; set; } = 2 * 1024 * 1024;
        public int QdrantRequestTimeoutSeconds { get; set; } = 180;
        public int GeminiRequestTimeoutSeconds { get; set; } = 30;
        public int ProcessingTimeoutSeconds { get; set; } = 120;
        public int UploadQueueCapacity { get; set; } = 2;
        public string UploadQueuePath { get; set; } = "App_Data/rag-upload-queue";
        public int GeminiThrottleDelayMs { get; set; } = 1000;
        public int QdrantThrottleDelayMs { get; set; } = 100;
        public int HttpMaxRetries { get; set; } = 5;
        public int HttpRetryBaseDelayMs { get; set; } = 1000;
        public int TopK { get; set; } = 5;

        public float MinScore { get; set; } = 0.25f;
    }
}