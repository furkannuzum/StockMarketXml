// StockMarket.Api/Models/StockUserPreference.cs
using System;
using System.ComponentModel.DataAnnotations; // Gerekirse validasyon için

namespace StockMarket.Api.Models
{
    public class StockUserPreference
    {
        [Key] // Eğer bir veritabanı kullanacak olsaydık birincil anahtar olurdu
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } = string.Empty; // Hangi kullanıcıya ait (User.Id'den gelecek)

        [Required]
        [MaxLength(10)]
        public string TickerSymbol { get; set; } = string.Empty;

        [MaxLength(500)] // Notlar için maksimum uzunluk
        public string? Notes { get; set; }

        public decimal? PriceAlertUpper { get; set; } // Null olabilir

        public decimal? PriceAlertLower { get; set; } // Null olabilir

        public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
    }
}