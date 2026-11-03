using AUIR_Software.Models.LLM;
using Microsoft.Extensions.Options;

public class OllamaAIchatProvider : ILLMchatProvider
{
    private readonly OllamaConfig _config;
    private readonly HttpClient _httpClient;

    public OllamaAIchatProvider(IOptions<AppConfig> config, HttpClient httpClient)
    {
        _config = config.Value.Ollama;
        _httpClient = httpClient;
    }

    public async Task<string> AskAsync(string system, string user, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _config.Model,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            },
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.BaseUrl}/api/chat",
            request,
            cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        return json;
    }
}