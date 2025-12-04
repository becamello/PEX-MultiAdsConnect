using System.Text.Json;
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
        private readonly IPdfService _pdfService;

        public SpreadsheetController(ISpreadsheetService service, IPdfService pdfService)
        {
            _service = service;
            _pdfService = pdfService;
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

                string geminiRaw = result.GeminiResponse switch
                {
                    null => string.Empty,
                    string s => s, 
                    JsonElement je => je.GetRawText(),
                    object o => JsonSerializer.Serialize(o) 
                };

                string textoLimpo = ExtractTextFromGeminiResponse(geminiRaw);

                var pdfBytes = _pdfService.GenerateReportPdf(textoLimpo);

                return File(pdfBytes, "application/pdf", "analise.pdf");
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

        private static string ExtractTextFromGeminiResponse(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
                return "(Sem conteúdo)";

            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                // Estrutura esperada: { "candidates": [ { "content": { "parts": [ { "text": "..." } ] } } ] }
                if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array && candidates.GetArrayLength() > 0)
                {
                    var first = candidates[0];

                    if (first.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.ValueKind == JsonValueKind.Array &&
                        parts.GetArrayLength() > 0)
                    {
                        var part0 = parts[0];
                        if (part0.TryGetProperty("text", out var textProp))
                        {
                            return textProp.GetString() ?? rawJson;
                        }
                    }

                    // às vezes a resposta está em candidates[0].content.parts[0].text como string direta
                    if (first.TryGetProperty("text", out var t2))
                        return t2.GetString() ?? rawJson;
                }

                // alternativa comum: {"candidates":[{"content":{"parts":[{"text":"..."}]}}], "modelVersion": "..."}
                // Se não achar, tente buscar por "text" em profundidade (último recurso)
                var maybeText = FindFirstPropertyRecursive(root, "text");
                if (!string.IsNullOrEmpty(maybeText))
                    return maybeText;

                // fallback: retorna o raw JSON (mas não ideal)
                return rawJson;
            }
            catch
            {
                // se não for JSON válido, considera que já é texto
                return rawJson;
            }
        }

        private static string? FindFirstPropertyRecursive(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                    return prop.GetString();

                foreach (var p in element.EnumerateObject())
                {
                    var found = FindFirstPropertyRecursive(p.Value, propertyName);
                    if (!string.IsNullOrEmpty(found))
                        return found;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var found = FindFirstPropertyRecursive(item, propertyName);
                    if (!string.IsNullOrEmpty(found))
                        return found;
                }
            }

            return null;
        }
    }
}
