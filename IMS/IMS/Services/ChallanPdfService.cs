using IMS.Models.DTO;
using System.Xml.Linq;
using System.Xml.Xsl;
using System.Xml;
using Newtonsoft.Json;
using SelectPdf; // Changed from Select.HtmlToPdf
using Microsoft.AspNetCore.Hosting;

namespace IMS.Services
{
    public interface IChallanPdfService
    {
        Task<string> GenerateChallanPdfAsync(DeliveryChallanDto challan);

        string GetPublicPdfUrl(string pdfFilePath);

    }

    public class ChallanPdfService : IChallanPdfService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ChallanPdfService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public ChallanPdfService(IWebHostEnvironment env, ILogger<ChallanPdfService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GenerateChallanPdfAsync(DeliveryChallanDto challan)
        {
            try
            {
                _logger.LogInformation($"Starting PDF generation for challan ID: {challan.ChallanNo}");

                var rootPath = Path.Combine(_env.WebRootPath, "challan-pdf");
                var xmlDir = Path.Combine(rootPath, "xml");
                var xslDir = Path.Combine(rootPath, "xsl");
                var pdfDir = Path.Combine(rootPath, "pdf");

                // Create directories if they don't exist
                Directory.CreateDirectory(xmlDir);
                Directory.CreateDirectory(xslDir);
                Directory.CreateDirectory(pdfDir);

                // 1️⃣ Convert DTO to XML and save
                var xmlPath = Path.Combine(xmlDir, $"Challan_{challan.ChallanNo}.xml");
                string xmlString = ConvertToXml(challan, "DeliveryChallan");
                await File.WriteAllTextAsync(xmlPath, xmlString);
                _logger.LogInformation($"XML saved at: {xmlPath}");

                // 2️⃣ Transform XML → HTML using XSLT
                var htmlPath = Path.Combine(xmlDir, $"Challan_{challan.ChallanNo}.html");
                var xslPath = Path.Combine(xslDir, "DeliveryChallan.xsl");

                if (!File.Exists(xslPath))
                {
                    throw new FileNotFoundException($"XSL file not found at: {xslPath}");
                }

                var xslt = new XslCompiledTransform();
                xslt.Load(xslPath);

                using (var xmlReader = XmlReader.Create(xmlPath))
                using (var writer = XmlWriter.Create(htmlPath, new XmlWriterSettings { Indent = true }))
                {
                    xslt.Transform(xmlReader, writer);
                }
                _logger.LogInformation($"HTML generated at: {htmlPath}");

                // 3️⃣ Convert HTML → PDF using SelectPdf
                var pdfPath = Path.Combine(pdfDir, $"Challan_{challan.ChallanNo}.pdf");
                string htmlContent = await File.ReadAllTextAsync(htmlPath);

                // Convert logo to Base64
                var logoPath = Path.Combine(_env.WebRootPath, "images", "kava.jpg");
                var logoBytes = await File.ReadAllBytesAsync(logoPath);
                var base64Logo = Convert.ToBase64String(logoBytes);

                // Replace src="/images/kava.jpg" with Base64
                htmlContent = htmlContent.Replace("/images/kava.jpg", $"data:image/jpeg;base64,{base64Logo}");


                // Create PDF converter
                var converter = new HtmlToPdf();

                // Set converter options (optional)
                converter.Options.PdfPageSize = PdfPageSize.A4;
                converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
                converter.Options.MarginTop = 10;
                converter.Options.MarginBottom = 10;
                converter.Options.MarginLeft = 10;
                converter.Options.MarginRight = 10;

                // Convert HTML to PDF
                PdfDocument doc = converter.ConvertHtmlString(htmlContent);

                // Save PDF
                doc.Save(pdfPath);
                doc.Close();

                _logger.LogInformation($"PDF generated successfully at: {pdfPath}");

                return pdfPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating PDF for challan ID: {challan.Id}");
                throw;
            }
        }

        private string ConvertToXml<T>(T obj, string rootName = "Root")
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
                var node = JsonConvert.DeserializeXNode(json, rootName);
                return node?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting object to XML");
                throw;
            }
        }

        public string GetPublicPdfUrl(string pdfFilePath)
        {
            // Example: pdfFilePath = "wwwroot/pdfs/challan_123.pdf"
            // Convert to URL: https://yourdomain.com/pdfs/challan_123.pdf

            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            // Remove wwwroot from path for public URL
            var relativePath = pdfFilePath.Replace(_env.WebRootPath, "").Replace("\\", "/");

            return $"{baseUrl}{relativePath}";
        }
    }
}