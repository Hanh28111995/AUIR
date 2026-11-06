using AUIR_Software.Models.LLM;
using AUIR_Software.Models.DTO;
using AUIR_Software.Services;
using AUIR_Software.Services.Vector;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AUIR_Software.Controllers.Home.ChatController
{
    /// <summary>Quản lý tài liệu RAG. Các endpoint yêu cầu cookie đăng nhập Admin.</summary>
    [Microsoft.AspNetCore.Mvc.Route("api/querry")]
    [ApiController]
    [Authorize]
    public class IngestController : ControllerBase
    {
        private readonly RagPipeline _rag;
        private readonly RagUploadQueue _uploadQueue;
        private readonly AppConfig _cgf;
        private readonly ILogger<IngestController> _logger;
        private readonly IAntiforgery _antiforgery;

        public IngestController(
            RagPipeline rag,
            RagUploadQueue uploadQueue,
            AppConfig cgf,
            ILogger<IngestController> logger,
            IAntiforgery antiforgery)
        {
            _rag = rag;
            _uploadQueue = uploadQueue;
            _cgf = cgf;
            _logger = logger;
            _antiforgery = antiforgery;
        }


        [HttpGet("upload-token")]
        [ProducesResponseType(typeof(AntiforgeryTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public ActionResult<AntiforgeryTokenResponse> GetUploadToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new AntiforgeryTokenResponse(tokens.RequestToken!));
        }

        [HttpPost("upload/{jobId}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelUpload(string jobId, CancellationToken ct)
        {
            var cancelled = await _uploadQueue.CancelAsync(jobId, ct);
            return cancelled
                ? NoContent()
                : NotFound(new ApiErrorResponse("Upload job not found or is already finished."));
        }
        
        [HttpPost("upload")]
        [ValidateAntiForgeryToken]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(210L * 1024L * 1024L)]
        [RequestFormLimits(MultipartBodyLengthLimit = 200L * 1024L * 1024L)]
        [RequestTimeout("RagUpload")]
        [ProducesResponseType(typeof(UploadAcceptedResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(UploadErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(UploadErrorResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status504GatewayTimeout)]
        public async Task<IActionResult> Upload(
            [FromForm] List<IFormFile> files,
            [FromHeader(Name = "RequestVerificationToken")] string? requestVerificationToken,
            CancellationToken ct)
        {
            var traceId = HttpContext.TraceIdentifier;
            var totalBytes = files?.Sum(file => file.Length) ?? 0;
            _logger.LogInformation(
                "RAG upload staging started. TraceId={TraceId} FileCount={FileCount} TotalBytes={TotalBytes} WorkingSetBytes={WorkingSetBytes}",
                traceId,
                files?.Count ?? 0,
                totalBytes,
                GetWorkingSetBytes());

            if (files == null || files.Count == 0)
            {
                return BadRequest(new UploadErrorResponse("No files uploaded.", []));
            }
            if (_cgf.Rag.ChunkSize <= _cgf.Rag.ChunkOverlap || _cgf.Rag.ChunkOverlap <= 0)
            {
                return BadRequest(new UploadErrorResponse(
                    "Invalid RAG chunk configuration: ChunkSize must be greater than ChunkOverlap, and ChunkOverlap must be greater than zero.",
                    []));
            }

            var acceptedFiles = new List<(IFormFile File, string Source)>();
            var skippedFiles = new List<RagUploadSkippedFile>();
            var seenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is not (".txt" or ".pdf" or ".md"))
                {
                    skippedFiles.Add(new RagUploadSkippedFile(file.FileName, "Unsupported file type. Supported: .pdf, .txt, .md."));
                    continue;
                }

                var source = Path.GetFileName(file.FileName);
                if (string.IsNullOrWhiteSpace(source))
                {
                    skippedFiles.Add(new RagUploadSkippedFile(file.FileName, "Invalid file name."));
                    continue;
                }
                if (!seenSources.Add(source))
                {
                    skippedFiles.Add(new RagUploadSkippedFile(file.FileName, "Duplicate file name in this upload."));
                    continue;
                }

                acceptedFiles.Add((file, source));
            }

            if (acceptedFiles.Count == 0)
            {
                return BadRequest(new UploadErrorResponse(
                    "No supported files were uploaded. Supported file types: .pdf, .txt, .md.",
                    skippedFiles));
            }

            RagUploadJob job;
            try
            {
                job = await _uploadQueue.EnqueueAsync(acceptedFiles, skippedFiles, ct);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "RAG upload queue rejected a job. TraceId={TraceId}", traceId);
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    new UploadErrorResponse(ex.Message, skippedFiles));
            }
            _logger.LogInformation(
                "RAG upload staged and queued. TraceId={TraceId} JobId={JobId} FileCount={FileCount} TotalBytes={TotalBytes} WorkingSetBytes={WorkingSetBytes}",
                traceId,
                job.Id,
                job.TotalFiles,
                totalBytes,
                GetWorkingSetBytes());

            return Ok(new UploadAcceptedResponse(
                job.Id,
                job.Status,
                $"/api/querry/upload/{job.Id}",
                job.TotalFiles,
                job.SkippedFiles));
        }

        /// <summary>Lấy trạng thái xử lý nền của một lần upload đã trả về JobId.</summary>
        [HttpGet("upload/{jobId}")]
        [ProducesResponseType(typeof(UploadJobStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUploadStatus(string jobId, CancellationToken ct)
        {
            var job = await _uploadQueue.GetJobAsync(jobId, ct);
            if (job is null)
            {
                return NotFound(new ApiErrorResponse("Upload job not found."));
            }

            return Ok(new UploadJobStatusResponse(
                job.Id,
                job.Status,
                job.Stage,
                job.CreatedAtUtc,
                job.UpdatedAtUtc,
                job.TotalFiles,
                job.CompletedFiles,
                job.IngestedChunks,
                job.SkippedFiles,
                job.Error));
        }

        private static long GetWorkingSetBytes()
        {
            using var process = Process.GetCurrentProcess();
            return process.WorkingSet64;
        }

        
        [HttpGet("documents")]
        [ProducesResponseType(typeof(List<RagDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ListDocuments(CancellationToken ct)
        {
            return Ok(await _rag.ListDocumentsAsync(ct));
        }

        
        [HttpDelete("documents")]
        [ValidateAntiForgeryToken]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDocument([FromQuery] string source, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return BadRequest("A document source is required.");
            }

            await _rag.DeleteDocumentAsync(source, ct);
            return NoContent();
        }

        /// <summary>Antiforgery token dùng cho các thao tác thay đổi dữ liệu.</summary>
        public sealed record AntiforgeryTokenResponse(string RequestVerificationToken);

        /// <summary>Kết quả nạp tài liệu vào kho RAG.</summary>
        public sealed record UploadAcceptedResponse(
            string JobId,
            string Status,
            string StatusUrl,
            int Files,
            IReadOnlyList<RagUploadSkippedFile> SkippedFiles);

        /// <summary>Tiến độ và kết quả của job upload.</summary>
        public sealed record UploadJobStatusResponse(
            string JobId,
            string Status,
            string Stage,
            DateTimeOffset CreatedAtUtc,
            DateTimeOffset UpdatedAtUtc,
            int TotalFiles,
            int CompletedFiles,
            int IngestedChunks,
            IReadOnlyList<RagUploadSkippedFile> SkippedFiles,
            string? Error);

        /// <summary>Lỗi upload cùng danh sách file không xử lý được.</summary>
        public sealed record UploadErrorResponse(
            string Message,
            IReadOnlyList<RagUploadSkippedFile> SkippedFiles);

    }
}
