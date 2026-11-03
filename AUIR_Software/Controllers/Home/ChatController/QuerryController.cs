using AUIR_Software.Models.DTO;
using AUIR_Software.Models.LLM;
using AUIR_Software.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AUIR_Software.Controllers.Home
{
    [Route("api/querry")]
    [ApiController]
    public class QuerryController : ControllerBase
    {
        private readonly RagPipeline _rag;
        private readonly AppConfig _cfg;

        public QuerryController(RagPipeline rag, AppConfig cfg)
        {
            _rag = rag;
            _cfg = cfg;
        }
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] QueryRequest request, CancellationToken cancellationToken)
        {
            var answer = await _rag.AskAsync(request.Question, _cfg.Rag.TopK ,cancellationToken);
            return Ok(new
            {
                success = true,
                response = answer
            });
        }
    }
}
