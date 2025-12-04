using Microsoft.AspNetCore.Http;

namespace MultiAdsConnect.Models
{
    public class SpreadsheetUploadRequest
    {
        public required IFormFile File { get; set; }
    }
}
