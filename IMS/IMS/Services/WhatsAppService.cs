namespace IMS.Services
{
    using global::IMS.Data;
    using global::IMS.Models;
    using global::IMS.Worker;
    // WhatsAppService.cs
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System.Text;

    namespace IMS.Services
    {
        public interface IWhatsAppService
        {
            string FormatPhoneNumber(string phoneNumber);
            string GeneratePdfDownloadLink(string ChallanNo);
            Task<WhatsAppSendResult> SendWhatsAppMessageAsync(WhatsAppQueue message);
        }

        public class WhatsAppSendResult
        {
            public bool Success { get; set; }
            public string Response { get; set; }
        }
        public class WhatsAppService : IWhatsAppService
        {
            private readonly IConfiguration _configuration;
            private readonly ILogger<WhatsAppService> _logger;
            private readonly IServiceScopeFactory _scopeFactory;

            public WhatsAppService(IConfiguration configuration, ILogger<WhatsAppService> logger, IServiceScopeFactory scopeFactory)
            {
                _configuration = configuration;
                _logger = logger;
                _scopeFactory = scopeFactory;
            }

            public string FormatPhoneNumber(string phoneNumber)
            {
                // Remove any non-digit characters
                var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

                // If number starts with 91 (India code) and has 12 digits, it's already formatted
                if (digitsOnly.StartsWith("91") && digitsOnly.Length == 12)
                {
                    return digitsOnly;
                }

                // If number has 10 digits, assume it's Indian number and add 91
                if (digitsOnly.Length == 10)
                {
                    return "91" + digitsOnly;
                }

                // If number has 12 digits but doesn't start with 91, check if it's valid
                if (digitsOnly.Length == 12 && !digitsOnly.StartsWith("91"))
                {
                    // You might want to handle other country codes here
                    return digitsOnly;
                }

                // Return as-is if already formatted or unknown format
                return digitsOnly;
            }

            public string GeneratePdfDownloadLink(string ChallanNo)
            {
                // For development, use localhost
                if (_configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
                {
                    var port = _configuration["ASPNETCORE_URLS"]?.Split(':').Last()?.Split('/').First() ?? "5000";
                    return $"http://localhost:{port}/api/challan/download-pdf/Challan_{ChallanNo}.pdf";
                }

                // For production, use configured domain
                var domain = _configuration["AppSettings:Domain"] ?? "https://yourdomain.com";
                return $"{domain}/api/challan/download-pdf/Challan_{ChallanNo}.pdf";
            }

            public async Task<WhatsAppSendResult> SendWhatsAppMessageAsync(WhatsAppQueue msg)
            {
                try
                {
                    // Format the phone number to E.164 format
                    string formattedNumber = FormatPhoneNumber(msg.MobileNumber);

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Get challan details
                    var challan = await db.DeliveryChallans
                        .Include(c => c.Items)
                        .FirstOrDefaultAsync(c => c.Id == msg.FkChallanId);

                    if (challan == null)
                    {
                        return new WhatsAppSendResult
                        {
                            Success = false,
                            Response = "Challan not found"
                        };
                    }

                    var pdfDownloadLink = GeneratePdfDownloadLink(challan.ChallanNo);

                    using var client = new HttpClient();
                    var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"] ?? "729036183634205";
                    var url = $"https://graph.facebook.com/v22.0/{phoneNumberId}/messages";
                    var accessToken = _configuration["WhatsApp:AccessToken"] ?? "EAASWRbgdvcIBPRODVP3PVdm51sR5FBTie5McqVzRMIodjDSdwv3Q5yLf50Iy9tZAV3SCUItVBdVFiRlGX7vQzaXKZCSGJuEcWn7ZBhNw4rW3TZCatCSwTRvN9gbgFxXrZBFHIScBW8l6eB3GqTAyYtrVYMwAvME91UGXLpZCEAqOIPvqc9mOesuZCBBS0QApZCyhuXH4x5XaO588wG9LpmqebVfRikZAzRkMpHcQ5d4L76QZDZD";

                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    var payload = new
                    {
                        messaging_product = "whatsapp",
                        to = formattedNumber,
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
                                        new { type = "text", text = challan.ReceiverName },
                                        new { type = "text", text = challan.ChallanNo },
                                        new { type = "text", text = challan.Date.ToString("dd-MMM-yyyy") },
                                        new { type = "text", text = challan.Items.Count.ToString() },
                                        new { type = "text", text = pdfDownloadLink }
                                    }
                                }
                            }
                        }
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);
                    var result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        return new WhatsAppSendResult { Success = true, Response = result };
                    }
                    else
                    {
                        _logger.LogError($"WhatsApp API error: {result}");
                        return new WhatsAppSendResult { Success = false, Response = result };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WhatsApp send error");
                    return new WhatsAppSendResult { Success = false, Response = ex.Message };
                }
            }
        }
    }
}
