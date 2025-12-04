using Microsoft.AspNetCore.Mvc;
using MultiAdsConnect.Models;
using MultiAdsConnect.Services;

namespace MultiAdsConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpreadsheetController : ControllerBase
    {
        private readonly ISpreadsheetService _service;

        public SpreadsheetController(ISpreadsheetService service)
        {
            _service = service;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(20_000_000)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] SpreadsheetUploadRequest request, CancellationToken ct)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { error = "Nenhum arquivo enviado." });

            var ext = Path.GetExtension(request.File.FileName).ToLower();
            if (ext != ".xls" && ext != ".xlsx")
                return BadRequest(new { error = "Arquivo precisa ser .xls ou .xlsx" });

            try
            {
                using var stream = request.File.OpenReadStream();
                var result = await _service.ProcessAndSendAsync(stream, request.File.FileName, ct);

                if (!result.Success)
                    return BadRequest(new { error = result.Message });

                return Ok(new
                {
                    message = result.Message,
                    gemini = result.GeminiResponse
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new { error = "Requisição cancelada." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
