// StockMarket.Api/Models/AuthDtos.cs
using System.ComponentModel.DataAnnotations;

namespace StockMarket.Api.Models
{
    public class UserRegisterDto
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)] // Şifre için minimum uzunluk
        public string Password { get; set; } = string.Empty;
    }

    public class UserLoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Bu satır var mı ve public mi?

    }
}