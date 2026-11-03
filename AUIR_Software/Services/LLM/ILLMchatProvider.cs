public interface ILLMchatProvider
{
    Task<string> AskAsync(string system, string user, CancellationToken cancellationToken = default);
}