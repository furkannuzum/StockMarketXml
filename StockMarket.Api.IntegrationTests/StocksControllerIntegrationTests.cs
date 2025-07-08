using Xunit;
using Microsoft.AspNetCore.Mvc.Testing; // WebApplicationFactory için
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers; // AuthenticationHeaderValue için
using System.Text.Json;       // JsonSerializer için
using System.Threading.Tasks;
using StockMarket.Api.Models;   // LoginResponseDto için
using StockMarket.Api;          // Program sınıfına erişim için (varsa)

namespace StockMarket.Api.IntegrationTests
{
    // Program sınıfını public yapmak veya InternalsVisibleTo kullanmak gerekebilir.
    // StockMarket.Api.csproj dosyasına <InternalsVisibleTo Include="StockMarket.Api.IntegrationTests" /> ekleyin.
    public class StocksControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>> // Program sınıfını kullan
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public StocksControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(); // Test sunucusu üzerinde çalışan bir HttpClient
        }

        private async Task<string?> GetJwtTokenAsync(string username, string password)
        {
            // Önce kullanıcıyı kaydet (test veritabanı her test için sıfırlanmıyorsa veya zaten varsa)
            // Veya doğrudan login ol. InMemoryUserRepository'de testuser/testpass var.
            var loginDto = new UserLoginDto { Username = username, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(loginDto), System.Text.Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v1/auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return loginResponse?.Token;
            }
            return null;
        }

        [Fact]
        public async Task GetAllStocks_WithoutToken_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/stocks");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAllStocks_WithValidToken_ReturnsSuccessAndXmlContent()
        {
            // Arrange
            var token = await GetJwtTokenAsync("testuser", "testpass"); // InMemoryUserRepository'deki kullanıcı
            Assert.NotNull(token); // Token alınabildi mi kontrol et
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));


            // Act
            var response = await _client.GetAsync("/api/v1/stocks");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("<StockDataFeed", responseString);
        }

        [Fact]
        public async Task GetStockReportHtml_ReturnsSuccessAndHtmlContent()
        {
            // Arrange (Bu endpoint [AllowAnonymous] olduğu için token gerekmiyor)
            _client.DefaultRequestHeaders.Accept.Clear(); // Önceki Accept header'ını temizle
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

            // Act
            var response = await _client.GetAsync("/api/v1/stocks/report/html");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            var responseString = await response.Content.ReadAsStringAsync();
            // Eğer AlphaVantage'dan veri gelmiyorsa, hata mesajı içeren HTML dönebilir.
            // Bu yüzden genel bir HTML varlığı kontrolü yapılabilir.
            Assert.Contains("<html", responseString, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}