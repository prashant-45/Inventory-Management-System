
//using IMS.Data;
//using IMS.Models;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Text;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace IMS.Worker
//{
//    public class WhatsAppSendResult
//    {
//        public bool Success { get; set; }
//        public string Response { get; set; }
//    }

//    public class WhatsAppWorker : BackgroundService
//    {
//        private readonly IServiceScopeFactory _scopeFactory;
//        private readonly ILogger<WhatsAppWorker> _logger;
//        private readonly IConfiguration _configuration;

//        public WhatsAppWorker(IServiceScopeFactory scopeFactory, ILogger<WhatsAppWorker> logger, IConfiguration configuration)
//        {
//            _scopeFactory = scopeFactory;
//            _logger = logger;
//            _configuration = configuration;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    using var scope = _scopeFactory.CreateScope();
//                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//                    var pendingMessages = await db.WhatsappMessageQueue
//                        .Where(m => m.Status == "PENDING")
//                        .OrderBy(m => m.CreatedAt)
//                        .Take(5)
//                        .ToListAsync(stoppingToken);

//                    foreach (var msg in pendingMessages)
//                    {
//                        try
//                        {
//                            var result = await SendWhatsAppMessage(msg);

//                            if (result.Success)
//                            {
//                                msg.Status = "SENT";
//                                msg.SentAt = DateTime.UtcNow;
//                                _logger.LogInformation($"WhatsApp message sent successfully to {msg.MobileNumber}");
//                            }
//                            else
//                            {
//                                msg.Status = "FAILED";
//                                msg.RetryCount++;
//                                msg.ErrorMessage = result.Response;
//                                _logger.LogError($"Failed to send WhatsApp message: {result.Response}");
//                            }

//                            await db.SaveChangesAsync(stoppingToken);
//                        }
//                        catch (Exception ex)
//                        {
//                            msg.Status = "FAILED";
//                            msg.ErrorMessage = ex.Message;
//                            msg.RetryCount++;
//                            _logger.LogError(ex, "Error sending WhatsApp message");
//                            await db.SaveChangesAsync(stoppingToken);
//                        }
//                    }

//                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Error in WhatsApp worker execution");
//                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
//                }
//            }
//        }

//        private async Task<WhatsAppSendResult> SendWhatsAppMessage(WhatsAppQueue msg)
//        {
//            try
//            {

//                // Format the phone number to E.164 format
//                string formattedNumber = FormatPhoneNumber(msg.MobileNumber);

//                using var scope = _scopeFactory.CreateScope();
//                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//                // Get challan details
//                var challan = await db.DeliveryChallans
//                    .Include(c => c.Items)
//                    .FirstOrDefaultAsync(c => c.Id == msg.FkChallanId);

//                if (challan == null)
//                {
//                    return new WhatsAppSendResult
//                    {
//                        Success = false,
//                        Response = "Challan not found"
//                    };
//                }

//                // Generate the PDF download link
//                // Generate development-friendly PDF link
//                var pdfDownloadLink = GeneratePdfDownloadLink(challan.Id);

//                using var client = new HttpClient();

//                // WhatsApp Cloud API endpoint - Use your actual phone number ID
//                var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"] ?? "729036183634205";
//                var url = $"https://graph.facebook.com/v22.0/{phoneNumberId}/messages";

//                // Get access token from configuration
//                var accessToken = _configuration["WhatsApp:AccessToken"] ?? "EAASWRbgdvcIBPRODVP3PVdm51sR5FBTie5McqVzRMIodjDSdwv3Q5yLf50Iy9tZAV3SCUItVBdVFiRlGX7vQzaXKZCSGJuEcWn7ZBhNw4rW3TZCatCSwTRvN9gbgFxXrZBFHIScBW8l6eB3GqTAyYtrVYMwAvME91UGXLpZCEAqOIPvqc9mOesuZCBBS0QApZCyhuXH4x5XaO588wG9LpmqebVfRikZAzRkMpHcQ5d4L76QZDZD";

//                client.DefaultRequestHeaders.Authorization =
//                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

//                // Create a simple text message with PDF link
//                var payload = new
//                {
//                    messaging_product = "whatsapp",
//                    to = formattedNumber,
//                    type = "text",
//                    text = new
//                    {
//                        body = $"Hello {challan.ReceiverName},\n\n" +
//                               $"Your delivery challan #{challan.ChallanNo} has been created successfully.\n\n" +
//                               $"📅 Date: {challan.Date:dd-MMM-yyyy}\n" +
//                               $"📦 Total Items: {challan.Items.Count}\n\n" +
//                               $"📄 Download your delivery challan PDF:\n{pdfDownloadLink}\n\n" +
//                               $"Thank you for your business! 🙏"
//                    }
//                };

//                var json = System.Text.Json.JsonSerializer.Serialize(payload);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await client.PostAsync(url, content);
//                var result = await response.Content.ReadAsStringAsync();

//                if (response.IsSuccessStatusCode)
//                {
//                    return new WhatsAppSendResult { Success = true, Response = result };
//                }
//                else
//                {
//                    _logger.LogError($"WhatsApp API error: {result}");
//                    return new WhatsAppSendResult { Success = false, Response = result };
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "WhatsApp send error");
//                return new WhatsAppSendResult { Success = false, Response = ex.Message };
//            }
//        }

//        public string GeneratePdfDownloadLink(int challanId)
//        {
//            // For development, use localhost
//            if (_configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
//            {
//                var port = _configuration["ASPNETCORE_URLS"]?.Split(':').Last()?.Split('/').First() ?? "5000";
//                return $"http://localhost:{port}/api/challan/download-pdf/Challan_{challanId}.pdf";
//            }

//            // For production, use configured domain
//            var domain = _configuration["AppSettings:Domain"] ?? "https://yourdomain.com";
//            return $"{domain}/api/challan/download-pdf/Challan_{challanId}.pdf";
//        }

//        public string FormatPhoneNumber(string phoneNumber)
//        {
//            // Remove any non-digit characters
//            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

//            // If number starts with 91 (India code) and has 12 digits, it's already formatted
//            if (digitsOnly.StartsWith("91") && digitsOnly.Length == 12)
//            {
//                return digitsOnly;
//            }

//            // If number has 10 digits, assume it's Indian number and add 91
//            if (digitsOnly.Length == 10)
//            {
//                return "91" + digitsOnly;
//            }

//            // If number has 12 digits but doesn't start with 91, check if it's valid
//            if (digitsOnly.Length == 12 && !digitsOnly.StartsWith("91"))
//            {
//                // You might want to handle other country codes here
//                return digitsOnly;
//            }

//            // Return as-is if already formatted or unknown format
//            return digitsOnly;
//        }
//    }
//}

// Updated WhatsAppWorker.cs
using IMS.Data;
using IMS.Services;
using IMS.Services.IMS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IMS.Worker
{
    public class WhatsAppWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WhatsAppWorker> _logger;
        private readonly IWhatsAppService _whatsAppService;

        public WhatsAppWorker(IServiceScopeFactory scopeFactory, ILogger<WhatsAppWorker> logger, IWhatsAppService whatsAppService)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _whatsAppService = whatsAppService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var pendingMessages = await db.WhatsappMessageQueue
                        .Where(m => m.Status == "PENDING")
                        .OrderBy(m => m.CreatedAt)
                        .Take(5)
                        .ToListAsync(stoppingToken);

                    foreach (var msg in pendingMessages)
                    {
                        try
                        {
                            var result = await _whatsAppService.SendWhatsAppMessageAsync(msg);

                            if (result.Success)
                            {
                                msg.Status = "SENT";
                                msg.SentAt = DateTime.UtcNow;
                                _logger.LogInformation($"WhatsApp message sent successfully to {msg.MobileNumber}");
                            }
                            else
                            {
                                msg.Status = "FAILED";
                                msg.RetryCount++;
                                msg.ErrorMessage = result.Response;
                                _logger.LogError($"Failed to send WhatsApp message: {result.Response}");
                            }

                            await db.SaveChangesAsync(stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            msg.Status = "FAILED";
                            msg.ErrorMessage = ex.Message;
                            msg.RetryCount++;
                            _logger.LogError(ex, "Error sending WhatsApp message");
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in WhatsApp worker execution");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
    }
}
