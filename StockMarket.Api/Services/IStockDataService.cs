// StockMarket.Api/Services/IStockDataService.cs
using StockMarket.Api.Models; // StockDataFeed ve Stock modellerimiz için
using System.Threading.Tasks; // Asenkron metotlar için

namespace StockMarket.Api.Services
{
    public interface IStockDataService
    {
        Task<StockDataFeed?> GetAllStocksAsync();
        Task<Stock?> GetStockByTickerAsync(string tickerSymbol);
        // İleride yeni hisse ekleme, güncelleme gibi metotlar da buraya eklenebilir
          // === YENİ METOTLAR ===
        Task<bool> AddTrackedSymbolAsync(string tickerSymbol);
        Task<bool> RemoveTrackedSymbolAsync(string tickerSymbol);
        Task<List<string>> GetTrackedSymbolsAsync(); // Takip edilen sembolleri listelemek için (opsiyonel)
    }
}