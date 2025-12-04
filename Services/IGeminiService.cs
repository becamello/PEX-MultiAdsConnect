namespace MultiAdsConnect.Services
{
    public interface IGeminiService
    {
        Task<string> AnalisarRelatorioAdsAsync(string relatorioJson);
    }
}
