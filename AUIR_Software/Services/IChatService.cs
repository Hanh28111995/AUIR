public interface IChatService
{
    Task<string> AskAsync(string userMessage);
}