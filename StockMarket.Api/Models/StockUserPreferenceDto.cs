// StockMarket.Api/Models/StockUserPreferenceDto.cs
using System.ComponentModel.DataAnnotations;

namespace StockMarket.Api.Models
{
    public class StockUserPreferenceCreateUpdateDto // Hem oluşturma hem güncelleme için kullanılabilir
    {
        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        [Range(0.01, 1000000, ErrorMessage = "Upper price alert must be a positive value.")] // Örnek bir aralık
        public decimal? PriceAlertUpper { get; set; }

        [Range(0.01, 1000000, ErrorMessage = "Lower price alert must be a positive value.")] // Örnek bir aralık
        public decimal? PriceAlertLower { get; set; }
    }

    public class StockUserPreferenceResponseDto // API'den dönerken kullanılacak DTO
    {
        public string TickerSymbol { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal? PriceAlertUpper { get; set; }
        public decimal? PriceAlertLower { get; set; }
        public DateTime LastModifiedUtc { get; set; }
    }
}