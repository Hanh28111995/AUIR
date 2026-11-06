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
            "Gemini"
                => _serviceProvider
                    .GetRequiredService<GeminiChatProvider>(),

            _ => throw new InvalidOperationException(
                $"LLM provider '{provider}' is not supported.")
        };
    }
}