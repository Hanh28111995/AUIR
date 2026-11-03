namespace AUIR_Software.Models.LLM
{
    public class AppConfig
    {
        public string Provider { get; set; } = "OpenAI";
        public OpenAIConfig OpenAI { get; set; } = new OpenAIConfig();
        public OllamaConfig Ollama { get; set; } = new OllamaConfig();
        public QdrantConfig Qdrant { get; set; } = new QdrantConfig();
        public RagConfig Rag { get; set; } = new RagConfig();



    }

    public class OpenAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ChatModel { get; set; } = "gpt-4o-mini";
        public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    }

    public class OllamaConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "llama3";
        public string EmbeddingModel { get; set; } = "nomic-embed-text";
    }

    public class QdrantConfig
    {
        public string Endpoint { get; set; } = "http://localhost:6333";
        public string Collection { get; set; } = "auir_collection";
    }

    public class RagConfig
    {
        public int ChunkSize { get; set; } = 880;
        public int ChunkOverlap { get; set; } = 120;
        public int TopK { get; set; } = 5;

        public float MinScore { get; set; } = 0.25f;
    }
}