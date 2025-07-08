// StockMarket.Api/Models/User.cs
namespace StockMarket.Api.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Şifreyi her zaman hash'lenmiş olarak saklayacağız
        public string Role { get; set; } = "User"; // Örnek rol: User, Admin vb.
    }
}