namespace MultiAdsConnect.Services
{
    public interface IPdfService
    {
        byte[] GenerateReportPdf(string reportText);
    }
}
