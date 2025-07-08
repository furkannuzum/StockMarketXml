// StockMarket.Api/Models/TickerSymbolDto.cs
using System.ComponentModel.DataAnnotations; // DataAnnotations (Required, StringLength vb.) için bu namespace gerekli

namespace StockMarket.Api.Models
{
    public class TickerSymbolDto
    {
        [Required(ErrorMessage = "Ticker symbol is required.")] // Bu alanın zorunlu olduğunu belirtir
        [RegularExpression("^[a-zA-Z0-9.-]+$", ErrorMessage = "Ticker symbol can only contain letters, numbers, dots (.), and hyphens (-).")] // Sembol formatını kontrol eder
        [StringLength(10, MinimumLength = 1, ErrorMessage = "Ticker symbol must be between 1 and 10 characters.")] // Uzunluk kısıtlaması
        public string TickerSymbol { get; set; } = string.Empty; // Başlangıç değeri atamak iyi bir pratiktir
    }
}