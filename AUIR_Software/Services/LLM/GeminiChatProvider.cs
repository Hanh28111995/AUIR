using System.Text.Json;
using AUIR_Software.Models.LLM;

public sealed class GeminiChatProvider : ILLMchatProvider
{
    private readonly GeminiApiClient _api;
    private readonly string _model;

    public GeminiChatProvider(GeminiApiClient api, AppConfig config)
    {
        _api = api;
        _model = config.Gemini.ChatModel;
    }

    public async Task<string> AskAsync(
        string system,
        string user,
        CancellationToken cancellationToken = default)
    {
        using var response = await _api.PostAsync(
            $"models/{Uri.EscapeDataString(_model)}:generateContent",
            new
            {
                systemInstruction = new { parts = new[] { new { text = system } } },
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = user } } }
                }
            },
            cancellationToken);

        return ReadText(response);
    }

    public async Task<string> AskWithToolAsync(
        string system,
        string user,
        string toolName,
        string toolDescription,
        string toolParametersJson,
        Func<string, CancellationToken, Task<string>> toolHandler,
        CancellationToken cancellationToken = default)
    {
        using var parameters = JsonDocument.Parse(toolParametersJson);
        var contents = new List<object>
        {
            new { role = "user", parts = new[] { new { text = user } } }
        };
        var tools = new[]
        {
            new
            {
                functionDeclarations = new[]
                {
                    new
                    {
                        name = toolName,
                        description = toolDescription,
                        parameters = parameters.RootElement
                    }
                }
            }
        };

        for (var turn = 0; turn < 8; turn++)
        {
            using var response = await _api.PostAsync(
                $"models/{Uri.EscapeDataString(_model)}:generateContent",
                new
                {
                    systemInstruction = new { parts = new[] { new { text = system } } },
                    contents,
                    tools,
                    toolConfig = new
                    {
                        functionCallingConfig = new { mode = "AUTO" }
                    }
                },
                cancellationToken);

            var candidate = GetCandidate(response);
            var modelContent = candidate.GetProperty("content");
            var parts = modelContent.GetProperty("parts");
            var functionCalls = parts
                .EnumerateArray()
                .Where(part => part.TryGetProperty("functionCall", out _))
                .ToList();

            if (functionCalls.Count == 0)
            {
                return ReadText(parts);
            }

            contents.Add(modelContent.Clone());
            var functionResponses = new List<object>();
            foreach (var part in functionCalls)
            {
                var functionCall = part.GetProperty("functionCall");
                var calledName = functionCall.GetProperty("name").GetString() ?? string.Empty;
                object result;

                if (calledName != toolName)
                {
                    result = new { error = $"Unknown tool: {calledName}" };
                }
                else
                {
                    var arguments = functionCall.TryGetProperty("args", out var args)
                        ? args.GetRawText()
                        : "{}";
                    var resultJson = await toolHandler(arguments, cancellationToken);
                    using var resultDocument = JsonDocument.Parse(resultJson);
                    result = resultDocument.RootElement.Clone();
                }

                functionResponses.Add(new
                {
                    functionResponse = new
                    {
                        name = calledName,
                        response = result
                    }
                });
            }

            contents.Add(new { role = "user", parts = functionResponses });
        }

        throw new InvalidOperationException(
            "Gemini exceeded the maximum number of tool-call turns.");
    }

    private static JsonElement GetCandidate(JsonDocument response)
    {
        if (!response.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini returned no response candidates.");
        }

        return candidates[0];
    }

    private static string ReadText(JsonDocument response)
    {
        var candidate = GetCandidate(response);
        return ReadText(candidate.GetProperty("content").GetProperty("parts"));
    }

    private static string ReadText(JsonElement parts)
    {
        return string.Join(
            string.Empty,
            parts.EnumerateArray()
                .Where(part => part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString()));
    }
}
