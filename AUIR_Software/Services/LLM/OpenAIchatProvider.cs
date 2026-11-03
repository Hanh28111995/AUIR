using OpenAI.Chat;

public class OpenAIchatProvider : ILLMchatProvider
{
    private readonly ChatClient _chatClient;

    public OpenAIchatProvider(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<string> AskAsync(string system, string user, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(system),
            new UserChatMessage(user)
        };
    
        var completion = await _chatClient.CompleteChatAsync(messages : messages, cancellationToken: cancellationToken);
        return completion.Value.Content[0].Text.ToString();
    }
}