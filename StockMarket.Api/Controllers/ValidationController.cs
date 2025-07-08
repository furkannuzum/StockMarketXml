// StockMarket.Api/Controllers/ValidationController.cs
using Microsoft.AspNetCore.Authorization; // [AllowAnonymous] için
using Microsoft.AspNetCore.Hosting;       // IWebHostEnvironment için
using Microsoft.AspNetCore.Http;        // StatusCodes için
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;     // ILogger için
using StockMarket.Api.Models;           // ValidationResultDto ve StockDataFeed için
using StockMarket.Api.Services;         // IStockDataService için
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;                      // Any() metodu için
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;                // XmlSchemaSet, XmlSchemaException için
using System.Xml.Serialization;         // XmlSerializer için

namespace StockMarket.Api.Controllers
{
    [ApiController]
    [Route("api/v1/validation")]
    [Produces("application/json")] // Bu controller validasyon sonuçlarını JSON olarak dönecek
    public class ValidationController : ControllerBase
    {
        private readonly IStockDataService _stockDataService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(
            IStockDataService stockDataService,
            IWebHostEnvironment env,
            ILogger<ValidationController> logger)
        {
            _stockDataService = stockDataService;
            _env = env;
            _logger = logger;
        }

        // GET: api/v1/validation/xsd-check-all-stocks
        [HttpGet("xsd-check-all-stocks")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ValidateAllStocksAgainstXsd()
        {
            _logger.LogInformation("ValidateAllStocksAgainstXsd endpoint çağrıldı.");
            var validationMessages = new List<string>();
            bool isValid = true;

            try
            {
                var stockDataFeed = await _stockDataService.GetAllStocksAsync();
                if (stockDataFeed == null || !stockDataFeed.Stocks.Any())
                {
                    return Ok(new ValidationResultDto { IsValid = false, Messages = new List<string> { "Valide edilecek hisse senedi verisi bulunamadı." } });
                }

                string xmlInput;
                var xmlSerializer = new XmlSerializer(typeof(StockDataFeed));
                var writerSettings = new XmlWriterSettings { Indent = false, Encoding = Encoding.UTF8 };
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, "http://www.example.com/stockdata/v1");

                using (var stringWriter = new StringWriter())
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
                    {
                        xmlSerializer.Serialize(xmlWriter, stockDataFeed, namespaces);
                        xmlInput = stringWriter.ToString();
                    }
                }
                _logger.LogDebug("XSD ile valide edilecek XML (ilk 500 karakter): {XmlContent}", xmlInput.Substring(0, Math.Min(xmlInput.Length, 500)));

                var schemas = new XmlSchemaSet();
                var xsdPath = Path.Combine(_env.ContentRootPath, "stockdata.xsd");
                if (!System.IO.File.Exists(xsdPath))
                {
                    _logger.LogError("XSD şema dosyası bulunamadı: {Path}", xsdPath);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ValidationResultDto { IsValid = false, Messages = new List<string> { $"XSD şema dosyası bulunamadı: {xsdPath}" } });
                }
                schemas.Add("http://www.example.com/stockdata/v1", xsdPath);

                var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = schemas };
                settings.ValidationEventHandler += (sender, e) =>
                {
                    isValid = false;
                    var message = $"XSD Validation Error ({e.Severity}): Line {e.Exception?.LineNumber}, Pos {e.Exception?.LinePosition} - {e.Message}";
                    validationMessages.Add(message);
                    _logger.LogWarning(message);
                };

                using (var stringReader = new StringReader(xmlInput))
                using (var xmlReader = XmlReader.Create(stringReader, settings))
                {
                    while (xmlReader.Read()) { }
                }

                if (isValid && !validationMessages.Any(m => m.ToUpper().Contains("ERROR"))) // Emin olmak için Error içeren mesaj var mı kontrolü
                {
                    if (!validationMessages.Any()) // Hiç mesaj yoksa geçerli mesajını ekle
                        validationMessages.Add("XML, XSD şemasına göre geçerlidir.");
                } else {
                    isValid = false; // Hata mesajı varsa veya isValid zaten false ise
                    if (!validationMessages.Any(m => m.ToUpper().Contains("ERROR")))
                        validationMessages.Add("XML, XSD şemasına göre GEÇERSİZDİR.");
                }
                return Ok(new ValidationResultDto { IsValid = isValid, Messages = validationMessages });
            }
            catch (XmlSchemaException schemaEx)
            {
                _logger.LogError(schemaEx, "XSD Şema yükleme/derleme hatası.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ValidationResultDto { IsValid = false, Messages = new List<string> { $"XSD Şema Hatası: {schemaEx.Message}" } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ValidateAllStocksAgainstXsd endpoint'inde genel bir hata oluştu.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ValidationResultDto { IsValid = false, Messages = new List<string> { $"Genel Hata: {ex.Message}" } });
            }
        }

        // === YENİ DTD VALIDASYON ENDPOINT'İ ===
        // GET: api/v1/validation/dtd-check-all-stocks
        [HttpGet("dtd-check-all-stocks")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ValidateAllStocksAgainstDtd()
        {
            _logger.LogInformation("ValidateAllStocksAgainstDtd endpoint çağrıldı.");
            var validationMessages = new List<string>();
            bool isValid = true;

            try
            {
                var stockDataFeed = await _stockDataService.GetAllStocksAsync();
                if (stockDataFeed == null || !stockDataFeed.Stocks.Any())
                {
                    return Ok(new ValidationResultDto { IsValid = false, Messages = new List<string> { "Valide edilecek hisse senedi verisi bulunamadı." } });
                }

                string xmlInput;
                var xmlSerializer = new XmlSerializer(typeof(StockDataFeed));
                
                // DTD'ler genellikle namespace'lerle iyi çalışmaz.
                // Eğer DTD'niz namespace'siz ise, serialize ederken namespace'leri kaldırmak
                // veya DTD'nizi namespace'e uygun hale getirmek gerekebilir.
                // Bu örnekte, XML'i namespace'siz serialize etmeyi deneyelim DTD için.
                var noNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
                var writerSettings = new XmlWriterSettings { Indent = false, Encoding = Encoding.UTF8, OmitXmlDeclaration = true };

                using (var stringWriter = new StringWriter())
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
                    {
                        // Namespace'siz serialize etmeyi deneyelim
                        xmlSerializer.Serialize(xmlWriter, stockDataFeed, noNamespaces);
                        xmlInput = stringWriter.ToString();
                    }
                }
                _logger.LogDebug("DTD için (muhtemelen namespace'siz) ham XML (ilk 500 karakter): {XmlContent}", xmlInput.Substring(0, Math.Min(xmlInput.Length, 500)));

                var dtdFileName = "stockdata.dtd";
                var dtdPath = Path.Combine(_env.ContentRootPath, dtdFileName);
                if (!System.IO.File.Exists(dtdPath))
                {
                    _logger.LogError("DTD dosyası bulunamadı: {Path}", dtdPath);
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new ValidationResultDto { IsValid = false, Messages = new List<string> { $"DTD dosyası bulunamadı: {dtdPath}" } });
                }

                // XML'in başına DOCTYPE tanımını ekle
                var doctypeString = $"<!DOCTYPE StockDataFeed SYSTEM \"{dtdFileName}\">\n"; // DTD dosyasının adı
                var fullXmlWithDoctype = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n{doctypeString}{xmlInput}";
                _logger.LogDebug("DTD validasyonu için DOCTYPE eklenmiş XML (ilk 600 karakter): {XmlContent}", fullXmlWithDoctype.Substring(0, Math.Min(fullXmlWithDoctype.Length, 600)));

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse,
                    ValidationType = ValidationType.DTD,
                    XmlResolver = new XmlUrlResolver() // Harici DTD'leri çözmek için
                };
                settings.ValidationEventHandler += (sender, e) =>
                {
                    isValid = false; // Hata veya uyarı durumunda isValid'i false yap
                    var message = $"DTD Validation ({e.Severity}): Line {e.Exception?.LineNumber}, Pos {e.Exception?.LinePosition} - {e.Message}";
                    validationMessages.Add(message);
                    _logger.LogWarning(message); // LogWarning veya LogError olabilir severity'e göre
                };

                using (var stringReader = new StringReader(fullXmlWithDoctype))
                {
                    using (var xmlReader = XmlReader.Create(stringReader, settings))
                    {
                        try
                        {
                            while (xmlReader.Read()) { }
                        }
                        catch (XmlException xmlEx) // DTD parse veya validasyon hataları XmlException fırlatabilir
                        {
                            isValid = false;
                            var message = $"DTD Processing/Parsing Error: {xmlEx.Message} (Line: {xmlEx.LineNumber}, Pos: {xmlEx.LinePosition})";
                            validationMessages.Add(message);
                            _logger.LogError(xmlEx, "DTD Processing/Parsing Error");
                        }
                    }
                }

                if (isValid && !validationMessages.Any(m => m.ToUpper().Contains("ERROR")))
                {
                     if (!validationMessages.Any()) // Hiç mesaj yoksa geçerli mesajını ekle
                        validationMessages.Insert(0, "XML, DTD'ye göre geçerlidir.");
                }
                else
                {
                    isValid = false; // Hata mesajı varsa veya isValid zaten false ise
                    if (!validationMessages.Any(m => m.ToUpper().Contains("ERROR")))
                        validationMessages.Add("XML, DTD'ye göre GEÇERSİZDİR veya uyarılar içeriyor.");
                }

                return Ok(new ValidationResultDto { IsValid = isValid, Messages = validationMessages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ValidateAllStocksAgainstDtd endpoint'inde genel bir hata oluştu.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ValidationResultDto { IsValid = false, Messages = new List<string> { $"Genel Hata: {ex.Message}" } });
            }
        }
    }

    // ValidationResultDto (Mevcut DTO'nuz)
     public class ValidationResultDto
     {
       public bool IsValid { get; set; }
         public List<string> Messages { get; set; } = new List<string>();
     }
}