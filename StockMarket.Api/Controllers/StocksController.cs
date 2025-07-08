// StockMarket.Api/Controllers/StocksController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockMarket.Api.Models; // TickerSymbolDto ve diğer modeller için
using StockMarket.Api.Services;
using System;
using System.Collections.Generic; // List<string> için
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.Xsl;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace StockMarket.Api.Controllers
{
    [ApiController]
    [Route("api/v1/stocks")]
    [Authorize] // Controller seviyesinde yetkilendirme
    public class StocksController : ControllerBase
    {
        private readonly IStockDataService _stockDataService;
        private readonly ILogger<StocksController> _logger;
        private readonly IWebHostEnvironment _env;

        public StocksController(
            IStockDataService stockDataService,
            ILogger<StocksController> logger,
            IWebHostEnvironment env)
        {
            _stockDataService = stockDataService ?? throw new ArgumentNullException(nameof(stockDataService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        // GET: api/v1/stocks
        [HttpGet]
        [Produces("application/xml")] // Bu endpoint XML üretecek
        [ProducesResponseType(typeof(StockDataFeed), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllStocks()
        {
            try
            {
                _logger.LogInformation("Authenticated user {User} called GetAllStocks endpoint.", User.Identity?.Name ?? "Anonymous");
                var stockDataFeed = await _stockDataService.GetAllStocksAsync();

                if (stockDataFeed == null || !stockDataFeed.Stocks.Any())
                {
                    _logger.LogWarning("Hiçbir hisse senedi verisi bulunamadı.");
                    // API'nin XML üretmesi beklendiği için NotFound durumunda da XML dönebiliriz.
                    return NotFound($"<Error xmlns=\"http://www.example.com/stockdata/v1\"><Message>Hisse senedi verisi bulunamadı.</Message></Error>");
                }
                return Ok(stockDataFeed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllStocks endpoint'inde bir hata oluştu.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"<Error xmlns=\"http://www.example.com/stockdata/v1\"><Message>Internal server error. Please try again later.</Message></Error>");
            }
        }

        // GET: api/v1/stocks/{ticker}
        [HttpGet("{ticker}")]
        [Produces("application/xml")] // Bu endpoint XML üretecek
        [ProducesResponseType(typeof(Stock), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStockByTicker(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                 return BadRequest($"<Error xmlns=\"http://www.example.com/stockdata/v1\"><Message>Ticker sembolü boş olamaz.</Message></Error>");
            }
            try
            {
                _logger.LogInformation("Authenticated user {User} called GetStockByTicker endpoint for Ticker: {Ticker}", User.Identity?.Name ?? "Anonymous", ticker);
                var stock = await _stockDataService.GetStockByTickerAsync(ticker);

                if (stock == null)
                {
                    _logger.LogWarning("Ticker sembolü '{Ticker}' için hisse senedi bulunamadı.", ticker);
                    return NotFound($"<Error xmlns=\"http://www.example.com/stockdata/v1\"><Message>'{ticker}' sembolüne sahip hisse senedi bulunamadı.</Message></Error>");
                }
                return Ok(stock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStockByTicker endpoint'inde bir hata oluştu. Ticker: {Ticker}", ticker);
                return StatusCode(StatusCodes.Status500InternalServerError, $"<Error xmlns=\"http://www.example.com/stockdata/v1\"><Message>Internal server error. Please try again later.</Message></Error>");
            }
        }

        // GET: api/v1/stocks/report/html
        [HttpGet("report/html")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStockReportHtml()
        {
            _logger.LogInformation("GetStockReportHtml endpoint çağrıldı.");
            try
            {
                var stockDataFeed = await _stockDataService.GetAllStocksAsync();
                if (stockDataFeed == null || !stockDataFeed.Stocks.Any())
                {
                    _logger.LogWarning("HTML raporu için hisse senedi verisi bulunamadı.");
                    return Content("<html><body><h1>Error</h1><p>No stock data available to generate the report.</p></body></html>", "text/html", Encoding.UTF8);
                }

                string xmlInput;
                var xmlSerializer = new XmlSerializer(typeof(StockDataFeed));
                var writerSettings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };
                using (var stringWriter = new StringWriter())
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
                    {
                        xmlSerializer.Serialize(xmlWriter, stockDataFeed);
                        xmlInput = stringWriter.ToString();
                    }
                }

                var xslt = new XslCompiledTransform();
                var xsltPath = Path.Combine(_env.ContentRootPath, "Transforms", "StocksToHtml.xslt");
                if (!System.IO.File.Exists(xsltPath))
                {
                    _logger.LogError("XSLT dosyası bulunamadı: {Path}", xsltPath);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error: Transformation file not found.");
                }
                xslt.Load(xsltPath);

                // YENİ: XSLT parametresi ekle
                var xsltArgs = new XsltArgumentList();
                var reportTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                xsltArgs.AddParam("reportGenerationTime", "", reportTime);

                using (var stringReader = new StringReader(xmlInput))
                using (var xmlReader = XmlReader.Create(stringReader))
                using (var stringWriterOutput = new StringWriter())
                {
                    // YENİ: XSLT argümanlarını kullan
                    xslt.Transform(xmlReader, xsltArgs, stringWriterOutput);
                    string htmlOutput = stringWriterOutput.ToString();
                    return Content(htmlOutput, "text/html", Encoding.UTF8);
                }
            }
            catch (XsltException xsltEx)
            {
                _logger.LogError(xsltEx, "XSLT dönüşümü sırasında hata oluştu.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"XSLT transformation error: {xsltEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStockReportHtml endpoint'inde bir hata oluştu.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error. Please try again later.");
            }
        }

        // GET: api/v1/stocks/query-with-xdocument
        [HttpGet("query-with-xdocument")]
        [Produces("application/xml")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueryStocksWithXDocument([FromQuery] decimal minPrice = 0)
        {
            // ... (bu metodun içeriği bir önceki mesajdaki gibi kalabilir, zaten doğruydu) ...
             _logger.LogInformation("QueryStocksWithXDocument endpoint called with minPrice: {MinPrice}", minPrice);
            if (minPrice < 0)
            {
                 return BadRequest(Content($"<Error xmlns=\"http://www.example.com/stockdata/v1\"><Message>minPrice cannot be negative.</Message></Error>", "application/xml"));
            }

            try
            {
                var stockDataFeed = await _stockDataService.GetAllStocksAsync();
                if (stockDataFeed == null || !stockDataFeed.Stocks.Any())
                {
                    return Ok(Content($"<NoDataAvailable xmlns=\"http://www.example.com/stockdata/v1\" />", "application/xml"));
                }

                string originalXmlString;
                var xmlSerializer = new XmlSerializer(typeof(StockDataFeed));
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, "http://www.example.com/stockdata/v1");

                using (var stringWriter = new StringWriter())
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Encoding = Encoding.UTF8, OmitXmlDeclaration = false, Indent = false }))
                    {
                        xmlSerializer.Serialize(xmlWriter, stockDataFeed, namespaces);
                        originalXmlString = stringWriter.ToString();
                    }
                }

                XDocument xDoc = XDocument.Parse(originalXmlString);
                XNamespace stockNs = "http://www.example.com/stockdata/v1";

                var filteredStockElements = xDoc.Descendants(stockNs + "Stock")
                                         .Where(stockElement =>
                                         {
                                             var priceElement = stockElement.Element(stockNs + "CurrentPrice");
                                             if (priceElement != null && decimal.TryParse(priceElement.Value, out decimal price))
                                             {
                                                 return price > minPrice;
                                             }
                                             return false;
                                         })
                                         .ToList();

                if (!filteredStockElements.Any())
                {
                    return Content($"<FilteredStockData xmlns=\"{stockNs}\"><Message>No stocks found with price greater than {minPrice}.</Message></FilteredStockData>", "application/xml", Encoding.UTF8);
                }

                XElement filteredRoot = new XElement(stockNs + "FilteredStockData");
                foreach (var stockNode in filteredStockElements)
                {
                    filteredRoot.Add(new XElement(stockNode));
                }
                
                return Content(filteredRoot.ToString(SaveOptions.None), "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "QueryStocksWithXDocument endpoint'inde bir hata oluştu.");
                return StatusCode(StatusCodes.Status500InternalServerError, Content($"<Error xmlns=\"http://www.example.com/stockdata/v1\"><Message>An error occurred while processing your request.</Message></Error>", "application/xml"));
            }
        }

        // === YENİ ADMİN ENDPOINT'LERİ ===

        // POST: api/v1/stocks/track
        [HttpPost("track")]
        [Authorize(Roles = "Admin")]
        [Consumes("application/json")] // JSON kabul eder
        [Produces("application/json")] // JSON döner (mesajlar için)
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> TrackNewSymbol([FromBody] TickerSymbolDto symbolDto)
        {
            if (!ModelState.IsValid) // DTO validasyonunu kontrol et
            {
                return BadRequest(ModelState); // ModelState hatalarını JSON olarak döner
            }
            // TickerSymbolDto içindeki [Required] zaten null/boş kontrolü yapar.
            // ModelState.IsValid false ise buraya düşer.

            _logger.LogInformation("Admin user {User} attempting to track symbol: {Symbol}", User.Identity?.Name ?? "Anonymous", symbolDto.TickerSymbol);
            bool addedNow = await _stockDataService.AddTrackedSymbolAsync(symbolDto.TickerSymbol);

            if (addedNow)
            {
                return Ok(new { message = $"Symbol '{symbolDto.TickerSymbol.ToUpperInvariant()}' is now being tracked." });
            }
            else
            {
                var currentSymbols = await _stockDataService.GetTrackedSymbolsAsync();
                if (currentSymbols.Contains(symbolDto.TickerSymbol.ToUpperInvariant()))
                {
                    return Ok(new { message = $"Symbol '{symbolDto.TickerSymbol.ToUpperInvariant()}' is already being tracked." });
                }
                // Eğer AddTrackedSymbolAsync false döndüyse ve listede de yoksa, başka bir sorun olabilir
                // (örn: sembol formatı vs. serviste daha detaylı kontrol edilebilir)
                return BadRequest(new { message = $"Could not track symbol '{symbolDto.TickerSymbol.ToUpperInvariant()}', it might be invalid or an unexpected error occurred." });
            }
        }

        // DELETE: api/v1/stocks/track/{ticker}
        [HttpDelete("track/{ticker}")]
        [Authorize(Roles = "Admin")]
        [Produces("application/json")] // JSON döner
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UntrackSymbol(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return BadRequest(new { message = "Ticker symbol cannot be empty." });
            }

            _logger.LogInformation("Admin user {User} attempting to untrack symbol: {Symbol}", User.Identity?.Name ?? "Anonymous", ticker);
            var success = await _stockDataService.RemoveTrackedSymbolAsync(ticker);

            if (success)
            {
                return Ok(new { message = $"Symbol '{ticker.ToUpperInvariant()}' is no longer being tracked." });
            }
            return NotFound(new { message = $"Symbol '{ticker.ToUpperInvariant()}' not found in tracking list or could not be removed." });
        }

        // GET: api/v1/stocks/tracked-symbols (Admin paneli için)
        [HttpGet("tracked-symbols")]
        [Authorize(Roles = "Admin")]
        [Produces("application/json")] // Bu JSON dönecek
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCurrentlyTrackedSymbols()
        {
            _logger.LogInformation("Admin user {User} fetching tracked symbols list.", User.Identity?.Name ?? "Anonymous");
            var symbols = await _stockDataService.GetTrackedSymbolsAsync();
            return Ok(symbols);
        }
    }
}