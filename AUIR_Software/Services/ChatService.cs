public class ChatService : IChatService
{
    private readonly ILLMProviderFactory _factory;

    public ChatService(ILLMProviderFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> AskAsync(string userMessage)
    {
        var provider = _factory.GetProvider();

        var systemPrompt = """
            You are AUIR assistant.
            You help users about software services.
            """;

        return await provider.AskAsync(
            systemPrompt,
            userMessage);
    }
}