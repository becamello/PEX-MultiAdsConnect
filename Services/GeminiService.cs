using System.Text.Json;
using System.Text;

namespace MultiAdsConnect.Services
{
    public class GeminiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string API_KEY = "AIzaSyCrQynCacYNkaq2E9vlKjMf0EekI4obejg";

        private const string MODEL = "gemini-2.5-flash";

        public GeminiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> AnalisarRelatorioAdsAsync(string relatorioJson)
        {
            var client = _httpClientFactory.CreateClient();

            string url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent?key={API_KEY}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new {
                                text = 
                                "Você é um analista sênior de tráfego pago. Analise detalhadamente o relatório de ads abaixo e gere:\n" +
                                "1) Análise geral\n" +
                                "2) Pontos de atenção\n" +
                                "3) Ajustes recomendados\n" +
                                "4) Ações práticas imediatas\n\n" +
                                "RELATÓRIO:\n" + relatorioJson
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }
    }
}
