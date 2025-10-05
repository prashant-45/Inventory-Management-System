using IMS.Services.IMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace IMS.Controllers
{
    [ApiController]
    [Route("api/challan")]
    [AllowAnonymous] // For testing, you might want to remove this in production
    public class ChallanPdfController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ChallanPdfController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWhatsAppService _whatsAppService ;



        public ChallanPdfController(IWebHostEnvironment env, ILogger<ChallanPdfController> logger, IConfiguration configuration, IWhatsAppService whatsAppService)
        {
            _env = env;
            _logger = logger;
            _configuration = configuration;
            _whatsAppService = whatsAppService;
        }

        [HttpGet("download-pdf/{challanNo}")]
        public IActionResult DownloadPdf(string challanNo)
        {
            try
            {
                var pdfPath = _whatsAppService.GeneratePdfDownloadLink(challanNo);//Path.Combine(_env.WebRootPath, "challan-pdf", "pdf", $"Challan_{challanId}.pdf");

                if (!System.IO.File.Exists(pdfPath))
                {
                    return NotFound($"PDF not found at: {pdfPath}");
                }

                var fileBytes = System.IO.File.ReadAllBytes(pdfPath);
                return File(fileBytes, "application/pdf", $"DeliveryChallan_{challanNo}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading PDF");
                return StatusCode(500, $"Error downloading PDF: {ex.Message}");
            }
        }
        //public string GeneratePdfDownloadLink(int challanId)
        //{
        //    // For development, use localhost
        //    if (_configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
        //    {
        //        var port = _configuration["ASPNETCORE_URLS"]?.Split(':').Last()?.Split('/').First() ?? "5000";
        //        return $"http://localhost:{port}/api/challan/download-pdf/Challan_{challanId}.pdf";
        //    }

        //    // For production, use configured domain
        //    var domain = _configuration["AppSettings:Domain"] ?? "https://yourdomain.com";
        //    return $"{domain}/api/challan/download-pdf/Challan_{challanId}.pdf";
        //}

        [HttpPost("test-template")]
        public async Task<IActionResult> TestTemplate([FromBody] TestTemplateRequest request)
        {
            using var client = new HttpClient();
            var accessToken = _configuration["WhatsApp:AccessToken"];
            var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var payload = new
            {
                messaging_product = "whatsapp",
                to = request.PhoneNumber,
                type = "template",
                template = new
                {
                    name = "delivery_challan_created",
                    language = new { code = "en" },
                    components = new object[]
                    {
                new
                {
                    type = "body",
                    parameters = new object[]
                    {
                        new { type = "text", text = "John Doe" },
                        new { type = "text", text = "TEST-123" },
                        new { type = "text", text = "14-Sep-2025" },
                        new { type = "text", text = "5" },
                        new { type = "text", text = "https://example.com/test.pdf" }
                    }
                }
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://graph.facebook.com/v23.0/{phoneNumberId}/messages";
            var response = await client.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            return Ok(new { Success = response.IsSuccessStatusCode, Response = result });
        }

        public class TestTemplateRequest
        {
            public string PhoneNumber { get; set; }
        }
    }
}
