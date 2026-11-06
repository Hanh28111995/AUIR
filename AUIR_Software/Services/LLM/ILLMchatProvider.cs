public interface ILLMchatProvider
{
    Task<string> AskAsync(string system, string user, CancellationToken cancellationToken = default);

    Task<string> AskWithToolAsync(
        string system,
        string user,
        string toolName,
        string toolDescription,
        string toolParametersJson,
        Func<string, CancellationToken, Task<string>> toolHandler,
        CancellationToken cancellationToken = default);
}