using Microsoft.AspNetCore.Mvc;
using MultiAdsConnect.Services;

namespace MultiAdsConnect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyzeController : ControllerBase
    {
        private readonly IGeminiService _geminiService;

        public AnalyzeController(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost("ads")]
        public async Task<IActionResult> AnalisarAds([FromBody] object report)
        {
            if (report == null)
                return BadRequest(new { error = "O relatório está vazio." });

            var jsonRelatorio = report.ToString() ?? "[]";
            var resposta = await _geminiService.AnalisarRelatorioAdsAsync(jsonRelatorio);
            return Ok(resposta);
        }
    }
}
