// StockMarket.Api/Services/IUserRepository.cs
using StockMarket.Api.Models;
using System.Threading.Tasks;

namespace StockMarket.Api.Services
{
    public interface IUserRepository
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task AddUserAsync(User user);
        // Task<bool> UserExistsAsync(string username); // İsteğe bağlı
    }
}   