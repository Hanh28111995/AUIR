using AUIR_Software.Models.DTO;
using AUIR_Software.Models.LLM;
using AUIR_Software.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AUIR_Software.Controllers.Home
{
    /// <summary>Thực hiện truy vấn RAG trực tiếp đến kho tài liệu.</summary>
    [Route("api/querry")]
    [ApiController]
    public class QuerryController : ControllerBase
    {
        private readonly RagPipeline _rag;
        private readonly AppConfig _cfg;
        private readonly ILogger<QuerryController> _logger;

        public QuerryController(RagPipeline rag, AppConfig cfg, ILogger<QuerryController> logger)
        {
            _rag = rag;
            _cfg = cfg;
            _logger = logger;
        }
        /// <summary>Tìm các đoạn tài liệu liên quan và tạo câu trả lời.</summary>
        /// <param name="request">Câu hỏi cần tra cứu.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        /// <response code="200">Trả về câu trả lời và các đoạn ngữ cảnh được tìm thấy.</response>
        /// <response code="400">Question bị thiếu hoặc không hợp lệ.</response>
        /// <response code="502">Không thể truy vấn dịch vụ embedding hoặc Qdrant.</response>
        [HttpPost]
        [ProducesResponseType(typeof(QueryApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Ask([FromBody] QueryRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var answer = await _rag.AskAsync(
                    request.Question,
                    request.Topk ?? _cfg.Rag.TopK,
                    cancellationToken);
                return Ok(new QueryApiResponse(true, answer));
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(exception, "RAG query failed while calling an upstream dependency.");
                return StatusCode(StatusCodes.Status502BadGateway, new ApiErrorResponse(
                    "Không thể truy vấn dịch vụ embedding hoặc Qdrant. Hãy kiểm tra log server."));
            }
        }
    }
}
