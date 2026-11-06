using AUIR_Software.Models.LLM;
using AUIR_Software.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AUIR_Software.Controllers.Home.ChatController;

/// <summary>Endpoint quản trị để kiểm tra trực tiếp Gemini, không gọi embedding hoặc Qdrant.</summary>
[ApiController]
[Route("api/diagnostics")]
[Authorize]
public sealed class GeminiDiagnosticsController : ControllerBase
{
    private readonly ILLMchatProvider _provider;
    private readonly AppConfig _config;
    private readonly ILogger<GeminiDiagnosticsController> _logger;

    public GeminiDiagnosticsController(
        ILLMProviderFactory factory,
        AppConfig config,
        ILogger<GeminiDiagnosticsController> logger)
    {
        _provider = factory.GetProvider();
        _config = config;
        _logger = logger;
    }

    /// <summary>Gửi một prompt kiểm tra ngắn trực tiếp đến model chat Gemini.</summary>
    /// <remarks>
    /// Chỉ kiểm tra kết nối và quyền gọi generateContent; không kiểm tra Gemini Embedding hoặc Qdrant.
    /// Yêu cầu cookie đăng nhập Admin trên cùng host với Swagger.
    /// </remarks>
    /// <param name="cancellationToken">Token hủy request.</param>
    /// <response code="200">Gemini đã phản hồi.</response>
    /// <response code="401">Chưa đăng nhập hoặc phiên đăng nhập đã hết hạn.</response>
    /// <response code="402">Prepaid credits của Gemini đã hết.</response>
    /// <response code="429">Gemini giới hạn tần suất hoặc quota.</response>
    /// <response code="502">Gemini từ chối request; kiểm tra model, API key và log server.</response>
    /// <response code="503">Gemini tạm thời không khả dụng.</response>
    /// <response code="504">Gemini không phản hồi trong thời gian cho phép.</response>
    [HttpPost("gemini")]
    [ProducesResponseType(typeof(GeminiDiagnosticResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<GeminiDiagnosticResponse>> CheckGemini(
        CancellationToken cancellationToken)
    {
        try
        {
            var reply = await _provider.AskAsync(
                "Reply with exactly the word OK.",
                "Reply with exactly the word OK.",
                cancellationToken);

            return Ok(new GeminiDiagnosticResponse(_config.Gemini.ChatModel, reply));
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Gemini diagnostic request timed out.");
            return StatusCode(StatusCodes.Status504GatewayTimeout, new ApiErrorResponse(
                "Gemini không phản hồi trong thời gian cho phép."));
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Gemini diagnostic request failed with status {StatusCode}.",
                exception.StatusCode);

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

            return StatusCode(StatusCodes.Status502BadGateway, new ApiErrorResponse(
                "Gemini từ chối request. Hãy kiểm tra model, API key và log server."));
        }
    }
}

/// <summary>Kết quả kiểm tra kết nối tới model Gemini.</summary>
public sealed record GeminiDiagnosticResponse(string Model, string Reply);
