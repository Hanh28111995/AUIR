using AUIR_Software.Models.LLM;
using AUIR_Software.Services;
using AUIR_Software.Services.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;

namespace AUIR_Software.Controllers.Home.ChatController
{
    [Microsoft.AspNetCore.Mvc.Route("api/querry")]
    [ApiController]
    public class IngestController : ControllerBase
    {
        private readonly RagPipeline _rag;
        private readonly AppConfig _cgf;

        public IngestController(RagPipeline rag, AppConfig cgf)
        {
            _rag = rag;
            _cgf = cgf;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 200L * 1024L * 1024L)]
        public async Task<IActionResult> Upload([FromForm] List<IFormFile> files, CancellationToken ct)
        {
            if (files == null || files.Count == 0) return BadRequest("No files uploaded.");
            var chunks = new List<(string text, string? source)>();
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is not (".txt" or ".pdf" or ".md")) continue;
                var tmp = Path.GetTempFileName();
                await using (var stream = System.IO.File.Create(tmp))
                    await file.CopyToAsync(stream, ct);
                string raw = ext switch
                {
                    ".pdf" => PdfTextExtractor.Extract(tmp),
                    ".txt" or ".md" => await System.IO.File.ReadAllTextAsync(tmp, ct),
                    _ => ""
                };
                System.IO.File.Delete(tmp);
                if (string.IsNullOrWhiteSpace(raw)) continue;
                foreach (var chunk in TextChunker.Chunk(raw, _cgf.Rag.ChunkSize, _cgf.Rag.ChunkOverlap))
                    chunks.Add((chunk, file.FileName));
            }
                if (chunks.Count() == 0) return BadRequest("No extractable text. Support: .pdf, .txt, .md");
                await _rag.IngestAsync(chunks, ct);
                return Ok(new { Ingested = chunks.Count, Files = files.Count });            
        }
    }
}
