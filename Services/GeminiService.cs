using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MultiAdsConnect.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private const string MODEL = "models/gemini-2.5-flash";

        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini:ApiKey não configurada.");
        }

        public async Task<string> AnalisarRelatorioAdsAsync(string relatorioJson)
        {
            var client = _httpClientFactory.CreateClient();

            var url = $"https://generativelanguage.googleapis.com/v1/{MODEL}:generateContent?key={_apiKey}";

            var prompt =
            """
                Você é um analista sênior de tráfego pago.
                Analise o relatório de campanhas em JSON e produza uma resposta:

                - Extremamente bem formatada em **Markdown**
                - Clara, objetiva e profissional
                - Com no máximo 20 a 30 linhas
                - Sem explicar fórmulas (CTR, CPC, CPM)
                - Sem introduções, apenas traga os tópicos solicitados
                - Foque em insights, não em teoria
                - Dê recomendações práticas que possam ser aplicadas imediatamente
                - Estruture exatamente nas seções abaixo:

                1. Visão Geral
                (resumo breve)

                2. Principais Indicadores
                (comparação dos pontos relevantes)

                3. Pontos de Atenção
                (itens críticos, diretos)

                4. Recomendações
                (passos claros e enxutos)

                Agora analise o JSON abaixo:
            """;

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt + relatorioJson
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }

    }
}
