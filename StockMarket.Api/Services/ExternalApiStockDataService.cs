// StockMarket.Api/Services/ExternalApiStockDataService.cs
using StockMarket.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StockMarket.Api.Services
{
    public class ExternalApiStockDataService : IStockDataService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExternalApiStockDataService> _logger;
        private readonly string _finnhubApiKey;
        private List<string> _trackedSymbols;

        private class FinnhubQuoteResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("c")]
            public decimal? CurrentPrice { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("d")]
            public decimal? Change { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("dp")]
            public decimal? PercentChange { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("h")]
            public decimal? HighPrice { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("l")]
            public decimal? LowPrice { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("o")]
            public decimal? OpenPrice { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("pc")]
            public decimal? PreviousClosePrice { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("t")]
            public long? Timestamp { get; set; }
        }

        private class FinnhubProfileResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string? Name { get; set; }
        }

        public ExternalApiStockDataService(HttpClient httpClient, IConfiguration configuration, ILogger<ExternalApiStockDataService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _finnhubApiKey = _configuration["Finnhub:ApiKey"]
                ?? throw new InvalidOperationException("Finnhub API Key not configured.");

            var initialSymbols = _configuration.GetSection("Finnhub:InitialTrackedSymbols").Get<List<string>>();
            _trackedSymbols = initialSymbols ?? new List<string> { "AAPL", "MSFT", "GOOGL", "TSLA" };
        }

        public async Task<StockDataFeed?> GetAllStocksAsync()
        {
            var stockDataFeed = new StockDataFeed();
            var symbolsToFetch = new List<string>(_trackedSymbols);

            foreach (var symbol in symbolsToFetch)
            {
                var stock = await GetStockByTickerAsync(symbol);
                if (stock != null)
                {
                    stockDataFeed.Stocks.Add(stock);
                }
                await Task.Delay(1100); // Finnhub rate limit consideration (approx. 1 request/sec)
            }

            return stockDataFeed.Stocks.Any() ? stockDataFeed : null;
        }

        public async Task<Stock?> GetStockByTickerAsync(string tickerSymbol)
        {
            if (string.IsNullOrWhiteSpace(tickerSymbol)) return null;
            if (string.IsNullOrEmpty(_finnhubApiKey) || _finnhubApiKey == "YOUR_FINNHUB_API_KEY_HERE")
            {
                _logger.LogWarning("Finnhub API key is not configured. Cannot fetch data for {Symbol}.", tickerSymbol);
                return null;
            }

            var quoteRequestUri = $"https://finnhub.io/api/v1/quote?symbol={tickerSymbol}&token={_finnhubApiKey}";
            var profileRequestUri = $"https://finnhub.io/api/v1/stock/profile2?symbol={tickerSymbol}&token={_finnhubApiKey}";

            try
            {
                var quoteResponse = await _httpClient.GetAsync(quoteRequestUri);
                quoteResponse.EnsureSuccessStatusCode();
                var quoteJsonResponse = await quoteResponse.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(quoteJsonResponse) || quoteJsonResponse == "{}") return null;

                var finnhubQuoteData = JsonSerializer.Deserialize<FinnhubQuoteResponse>(quoteJsonResponse);
                if (finnhubQuoteData == null || finnhubQuoteData.CurrentPrice == null)
                {
                    _logger.LogWarning("Finnhub API did not return valid data for {Symbol}.", tickerSymbol);
                    return null;
                }

                string companyName = $"Şirket Adı ({tickerSymbol})";
                long volume = 0;
                string description = $"{tickerSymbol} için Finnhub'dan alınan borsa verisi.";

                try
                {
                    var profileHttpResponse = await _httpClient.GetAsync(profileRequestUri);
                    if (profileHttpResponse.IsSuccessStatusCode)
                    {
                        var profileJsonResponse = await profileHttpResponse.Content.ReadAsStringAsync();
                        var profileData = JsonSerializer.Deserialize<FinnhubProfileResponse>(profileJsonResponse);
                        if (profileData != null && !string.IsNullOrEmpty(profileData.Name))
                        {
                            companyName = profileData.Name;
                        }
                    }
                }
                catch (Exception exProf)
                {
                    _logger.LogError(exProf, "Error while fetching profile data from Finnhub for {Symbol}.", tickerSymbol);
                }

                var stock = new Stock
                {
                    TickerSymbol = tickerSymbol.ToUpper(),
                    CompanyName = companyName,
                    CurrentPrice = finnhubQuoteData.CurrentPrice ?? 0,
                    Change = finnhubQuoteData.Change ?? 0,
                    PercentChange = (finnhubQuoteData.PercentChange ?? 0) / 100,
                    Volume = volume,
                    LastUpdated = finnhubQuoteData.Timestamp.HasValue ? DateTimeOffset.FromUnixTimeSeconds(finnhubQuoteData.Timestamp.Value).UtcDateTime : DateTime.UtcNow,
                    Description = description
                };

                return stock;
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "HttpRequestException while fetching data for {Symbol} from Finnhub.", tickerSymbol);
                return null;
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "JsonException while parsing Finnhub response for {Symbol}.", tickerSymbol);
                return null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected exception while fetching data for {Symbol}.", tickerSymbol);
                return null;
            }
        }

        public Task<bool> AddTrackedSymbolAsync(string tickerSymbol)
        {
            var symbolUpper = tickerSymbol.ToUpperInvariant();
            if (!_trackedSymbols.Contains(symbolUpper))
            {
                _trackedSymbols.Add(symbolUpper);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> RemoveTrackedSymbolAsync(string tickerSymbol)
        {
            var symbolUpper = tickerSymbol.ToUpperInvariant();
            return Task.FromResult(_trackedSymbols.Remove(symbolUpper));
        }

        public Task<List<string>> GetTrackedSymbolsAsync()
        {
            return Task.FromResult(new List<string>(_trackedSymbols));
        }
    }
}
