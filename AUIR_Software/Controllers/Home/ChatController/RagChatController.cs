using AUIR_Software.Services.Tool;
using AUIR_Software.Models.DTO;
using AUIR_Software.Models.LLM;
using AUIR_Software.Services.Chat;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;

namespace AUIR_Software.Controllers.Home.ChatController
{
    /// <summary>Yêu cầu chat với RAG; hai tham số tìm kiếm được lấy từ cấu hình backend.</summary>
    public sealed record ToolRagChatRequest
    {
        /// <summary>Câu hỏi cần gửi tới chatbot.</summary>
        [Required, StringLength(2000, MinimumLength = 1)]
        public required string Message { get; init; }

        public string? ConversationId { get; init; }
    }

    /// <summary>Phản hồi chat do Gemini tạo.</summary>
    public sealed record ChatAssistResponse(string Reply);

    /// <summary>Gửi câu hỏi tới Gemini; chatbot có thể tra cứu nội dung trong kho RAG.</summary>
    [Route("chat")]
    [ApiController]
    public class RagChatController : Controller
    {
        private readonly ILLMchatProvider _provider;
        private readonly ILogger<RagChatController> _logger;
        private readonly SearchDocTool _search;
        private readonly AppConfig _config;
        private readonly ChatQueryRouter _router;        
        private readonly ChatAnswerCache _cache;

        public RagChatController(
            ILLMProviderFactory factory,
            SearchDocTool search,
            AppConfig config,
            ChatQueryRouter router,
            ILogger<RagChatController> logger)
        {
            _provider = factory.GetProvider();
            _search = search;
            _config = config;
            _router = router;
            _logger = logger;
        }

        /// <summary>Trả lời một câu hỏi bằng Gemini và tìm kiếm tài liệu liên quan khi cần.</summary>
        /// <param name="request">Câu hỏi và tùy chọn tìm kiếm RAG.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        /// <response code="200">Gemini đã trả lời thành công.</response>
        /// <response code="400">Nội dung request không hợp lệ.</response>
        /// <response code="402">Prepaid credits của Gemini đã hết.</response>
        /// <response code="429">Gemini giới hạn tần suất hoặc quota.</response>
        /// <response code="502">Gemini hoặc quá trình truy xuất tài liệu gặp lỗi.</response>
        /// <response code="503">Gemini tạm thời không khả dụng.</response>
        /// <response code="504">Gemini không phản hồi trong thời gian cho phép.</response>
        [EnableRateLimiting("public-chat")]
        [HttpPost("assist")]
        [ProducesResponseType(typeof(ChatAssistResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status402PaymentRequired)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status504GatewayTimeout)]
        public async Task<IActionResult> Assist(
            [FromBody] ToolRagChatRequest request,
            CancellationToken cancellationToken)
        {
            var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
                ? HttpContext.TraceIdentifier
                : request.ConversationId.Trim();
            var lastHits = new List<(int Ordinal, string Source, double Score)>();
            var route = _router.Route(request.Message, conversationId);
            if (route.StaticReply is not null)
            {
                return Ok(new ChatAssistResponse(route.StaticReply));
            }

            try
            {
                var resultJson = await _search.InvokeAsync(
                    route.Query,
                    _config.Rag.TopK,
                    _config.Rag.MinScore,
                    route.SourceHint,
                    cancellationToken);
                lastHits.AddRange(ParseHits(resultJson));

                if (lastHits.Count == 0 && route.SourceHint is not null)
                {
                    resultJson = await _search.InvokeAsync(
                        route.Query,
                        _config.Rag.TopK,
                        _config.Rag.MinScore,
                        null,
                        cancellationToken);
                    lastHits.AddRange(ParseHits(resultJson));
                }

                if (lastHits.Count == 0)
                {
                    return Ok(new ChatAssistResponse(
                        "Thông tin hiện không có sẵn trên website. " +
                        "Vui lòng tới mục Contact trên website để liên hệ trực tiếp."));
                }

                _router.RememberSource(conversationId, lastHits[0].Source);
                var context = BuildContext(resultJson);
                var systemPrompt =
                    "Bạn là trợ lý hỏi đáp của AUIR. Chỉ trả lời dựa trên CONTEXT được cung cấp; " +
                    "nếu context không đủ thì nói rõ chưa tìm thấy thông tin. Không tự bịa. " +
                    "Trả lời ngắn gọn bằng tiếng Việt và dùng [#ordinal] khi trích dẫn.";
                var reply = await _provider.AskAsync(
                    systemPrompt,
                    $"CONTEXT:\n{context}\n\nCÂU HỎI:\n{request.Message}",
                    cancellationToken);

                //reply += BuildCitations(lastHits);
                return Ok(new ChatAssistResponse(reply));
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(exception, "Gemini request timed out.");
                return StatusCode(StatusCodes.Status504GatewayTimeout, new
                {
                    message = "Gemini phản hồi quá chậm. Vui lòng thử lại sau."
                });
            }
            catch (RagSearchException exception)
            {
                _logger.LogError(
                    exception,
                    "RAG document search failed during chat. Check Gemini embeddings and Qdrant availability.");
                return StatusCode(StatusCodes.Status502BadGateway, new ApiErrorResponse(
                    "Không thể tìm kiếm tài liệu. Hãy kiểm tra dịch vụ embedding và Qdrant."));
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(exception, "Gemini request failed with status {StatusCode}.", exception.StatusCode);

                if (exception.StatusCode == HttpStatusCode.PaymentRequired)
                {
                    return StatusCode(StatusCodes.Status402PaymentRequired, new ApiErrorResponse(
                        "Prepaid credits của Gemini đã hết. Hãy nạp thêm credit cho project đang gắn với GEMINI_KEY trong Google AI Studio."));
                }

                if (exception.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    return StatusCode(StatusCodes.Status429TooManyRequests, new ApiErrorResponse(
                        "Gemini đã giới hạn tần suất hoặc quota. Kiểm tra quota/API key rồi thử lại sau."));
                }

                if (exception.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiErrorResponse(
                        "Gemini tạm thời không khả dụng. Hãy thử lại sau."));
                }

                if (exception.Message.Contains("API_KEY_INVALID", StringComparison.Ordinal))
                {
                    return StatusCode(StatusCodes.Status502BadGateway, new ApiErrorResponse(
                        "Gemini từ chối API key. Hãy kiểm tra GEMINI_KEY rồi khởi động lại ứng dụng."));
                }

                return StatusCode(StatusCodes.Status502BadGateway, new ApiErrorResponse(
                    "Không thể hoàn tất yêu cầu với Gemini. Hãy kiểm tra model và log server."));
            }

        }

        private static string BuildContext(string resultJson)
        {
            using var document = JsonDocument.Parse(resultJson);
            return string.Join("\n", document.RootElement.GetProperty("hits").EnumerateArray()
                .Select(hit =>
                    $"[#{hit.GetProperty("ordinal").GetInt32()}] " +
                    $"{hit.GetProperty("source").GetString()}: " +
                    hit.GetProperty("snippet").GetString()));
        }

        private static string BuildCitations(List<(int Ordinal, string Source, double Score)> hits)
        {
            if (hits.Count == 0) return string.Empty;
            return "\n\nSources:\n" + string.Join("\n",
                hits.Select(hit => $"[#{hit.Ordinal}] {hit.Source} (score={hit.Score:0.000})"));
        }

        private static List<(int Ordinal, string Source, double Score)> ParseHits(string resultJson)
        {
            using var resultDocument = JsonDocument.Parse(resultJson);
            var hits = new List<(int Ordinal, string Source, double Score)>();
            foreach (var hit in resultDocument.RootElement.GetProperty("hits").EnumerateArray())
            {
                hits.Add((
                    hit.GetProperty("ordinal").GetInt32(),
                    hit.GetProperty("source").GetString() ?? "(unknown)",
                    hit.GetProperty("score").GetDouble()));
            }

            return hits;
        }

        private sealed class RagSearchException(Exception innerException)
            : Exception("RAG document search failed.", innerException)
        {
        }
    }
}
