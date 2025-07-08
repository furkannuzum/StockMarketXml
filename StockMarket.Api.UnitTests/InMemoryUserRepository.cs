using Xunit;
using StockMarket.Api.Models;
using StockMarket.Api.Services;
using System.Threading.Tasks;
using BCrypt.Net; // Eğer BCrypt kullanıyorsak

namespace StockMarket.Api.UnitTests
{
    public class InMemoryUserRepositoryTests
    {
        [Fact]
        public async Task AddUserAsync_ShouldAddUserToList()
        {
            // Arrange
            var userRepository = new InMemoryUserRepository(); // Test edilen sınıfın bir örneği
            var newUser = new User
            {
                Username = "testuser123",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), // BCrypt kullanılıyorsa
                Role = "User"
            };

            // Act
            await userRepository.AddUserAsync(newUser);
            var retrievedUser = await userRepository.GetUserByUsernameAsync("testuser123");

            // Assert
            Assert.NotNull(retrievedUser);
            Assert.Equal("testuser123", retrievedUser.Username);
            Assert.Equal(newUser.PasswordHash, retrievedUser.PasswordHash);
        }

        [Fact]
        public async Task GetUserByUsernameAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userRepository = new InMemoryUserRepository();

            // Act
            var retrievedUser = await userRepository.GetUserByUsernameAsync("nonexistentuser");

            // Assert
            Assert.Null(retrievedUser);
        }

        // InMemoryUserRepository içindeki static constructor'da eklenen kullanıcıları test edebiliriz
        [Fact]
        public async Task GetUserByUsernameAsync_ShouldReturnPredefinedUser_WhenUserExists()
        {
            // Arrange
            var userRepository = new InMemoryUserRepository(); // Static constructor çalışacak

            // Act
            var predefinedUser = await userRepository.GetUserByUsernameAsync("testuser"); // Varsayılan olarak eklenen

            // Assert
            Assert.NotNull(predefinedUser);
            Assert.Equal("testuser", predefinedUser.Username);
            Assert.True(BCrypt.Net.BCrypt.Verify("testpass", predefinedUser.PasswordHash));
        }
    }
}