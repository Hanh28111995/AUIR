using AUIR_Software.Services.Tool;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using System.Text.Json;

namespace AUIR_Software.Controllers.Home.ChatController
{
    public record ToolRagChatRequest(string Message, int? TopK, float? MinScore);

    [Route("chat")]
    [ApiController]
    public class RagChatController : Controller
    {
        private readonly ChatClient _chat;
        private readonly SearchDocTool _search;
        public RagChatController(ChatClient chat, SearchDocTool search)
        {
            _chat = chat;
            _search = search;
        }
        [HttpPost("assist")]
        public async Task<IActionResult> Assist([FromBody] ToolRagChatRequest request, CancellationToken cancellationToken)
        {
            var searchTool = ChatTool.CreateFunctionTool(
                functionName: nameof(SearchDocTool),
                functionDescription: "Search documents in the knowledge base",
                functionParameters: BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {
                    "query": { "type": "string", "description": "User question to search for" },
                    "topK": { "type": "integer", "minimum": 1, "maximum": 20 },
                    "minScore": { "type": "number", "minimum": -1, "maximum": 1 }
                },
                "required": [ "query" ],
                "additionalProperties": false
            }
            """u8.ToArray())
                );

            var messages = new List<ChatMessage>
        {
            new SystemChatMessage(
                "You are a retrieval-augmented assistant. " +
                $"Decide when to call {nameof(SearchDocTool)}. " +
                $"If the user query likely needs facts from our knowledge base, call {nameof(SearchDocTool)} first, " +
                "then answer ONLY using returned snippets. " +
                "Cite passages inline with [#ordinal] and list sources at the end. " +
                $"If {nameof(SearchDocTool)} returns no hits, say you don't know."
            ),
            new UserChatMessage(request.Message)
        };

            var options = new ChatCompletionOptions { Tools = { searchTool } };

            List<(int Ordinal, string Source, double Score)> lastHits = new();

            bool requiresAction;
            do
            {
                requiresAction = false;
                var completionResult = await _chat.CompleteChatAsync(messages, options, cancellationToken);
                var completion = completionResult.Value ?? completionResult;
                if (completion == null)
                    return BadRequest("No completion result.");

                switch (completion.FinishReason)
                {
                    case ChatFinishReason.Stop:
                        messages.Add(new AssistantChatMessage(completion));
                        var reply = completion.Content == null && completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;
                        reply += BuildCitations(lastHits);
                        return Ok(new { reply });

                    case ChatFinishReason.ToolCalls:
                        messages.Add(new AssistantChatMessage(completion));
                        if (completion.ToolCalls != null)
                        {
                            foreach (var call in completion.ToolCalls)
                                await HandleToolCall(call, request, messages, cancellationToken, lastHits);
                        }
                        requiresAction = true;
                        break;
                    default:
                        throw new NotImplementedException(completion.FinishReason.ToString());
                }
            } while (requiresAction);

            return BadRequest("Unexpected exit from chat loop.");
        }

        private static string BuildCitations(List<(int Ordinal, string Source, double Score)> hits)
        {
            if (hits.Count == 0) return string.Empty;
            return "\n\nSources:\n" + string.Join("\n",
                hits.Select(h => $"[#{h.Ordinal}] {h.Source} (score={h.Score:0.000})"));
        }

        private static List<(int Ordinal, string Source, double Score)> ParseHits(string resultJson)
        {
            var hits = new List<(int, string, double)>();
            try
            {
                using var resultDoc = JsonDocument.Parse(resultJson);
                var hitsEl = resultDoc.RootElement
                    .GetProperty("hits")
                    .EnumerateArray();
                foreach (var h in hitsEl)
                {
                    hits.Add((
                        Ordinal: h.GetProperty("ordinal").GetInt32(),
                        Source: h.GetProperty("source").GetString() ?? "(unknown)",
                        Score: h.GetProperty("score").GetDouble()
                    ));
                }
            }
            catch { /* Ignore parse errors */ }
            return hits;
        }

        private async Task HandleToolCall(ChatToolCall call, ToolRagChatRequest req, List<ChatMessage> messages, CancellationToken ct, List<(int Ordinal, string Source, double Score)> lastHits)
        {
            switch (call.FunctionName)
            {
                case nameof(SearchDocTool):
                    string jsonArgs = call.FunctionArguments.ToString();
                    using (var doc = JsonDocument.Parse(jsonArgs))
                    {
                        var root = doc.RootElement;
                        string query = root.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
                        int? topK = root.TryGetProperty("topK", out var k) ? k.GetInt32() : req.TopK;
                        float? minScore = root.TryGetProperty("minScore", out var s) ? (float?)s.GetDouble() : req.MinScore;

                        string resultJson = await _search.InvokeAsync(query, topK, minScore, ct);
                        lastHits.Clear();
                        lastHits.AddRange(ParseHits(resultJson));
                        messages.Add(new ToolChatMessage(call.Id, [ChatMessageContentPart.CreateTextPart(resultJson)]));
                    }
                    break;
                default:
                    messages.Add(new ToolChatMessage(call.Id, [ChatMessageContentPart.CreateTextPart($"{{ \"error\": \"Unknown tool: {call.FunctionName}\" }}")]));
                    break;
            }
        }

    }
}
