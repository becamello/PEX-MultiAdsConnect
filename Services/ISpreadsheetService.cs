namespace MultiAdsConnect.Services
{
    public interface ISpreadsheetService
    {
        Task<(bool Success, string Message, object? GeminiResponse)> ProcessAndSendAsync(
            Stream fileStream, string fileName, CancellationToken ct = default);
    }
}
