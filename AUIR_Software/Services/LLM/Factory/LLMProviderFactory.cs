public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public LLMProviderFactory(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public ILLMchatProvider GetProvider()
    {
        var provider =
            _configuration["LLM:Provider"];

        return provider switch
        {
            "OpenAI"
                => _serviceProvider
                    .GetRequiredService<OpenAIchatProvider>(),

            "Ollama"
                => _serviceProvider
                    .GetRequiredService<OllamaAIchatProvider>(),

            _ => throw new Exception(
                "Provider not supported")
        };
    }
}