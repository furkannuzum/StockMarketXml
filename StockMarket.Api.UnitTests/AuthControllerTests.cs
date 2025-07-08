using Xunit;
using Moq; // Moq için
using StockMarket.Api.Controllers;
using StockMarket.Api.Models;
using StockMarket.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace StockMarket.Api.UnitTests
{
    public class AuthControllerTests
    {
        // GenerateJwtToken metodu private olduğu için doğrudan test etmek zor.
        // Genellikle bu tür yardımcı metotlar ayrı bir servise (örn: ITokenService) taşınır ve o servis test edilir.
        // Şimdilik Login metodunun token döndürüp döndürmediğini kontrol edebiliriz.

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var mockUserRepo = new Mock<IUserRepository>();
            var mockConfig = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<AuthController>>();

            var testUser = new User
            {
                Id = System.Guid.NewGuid(),
                Username = "logintest",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("validpassword"),
                Role = "User"
            };

            mockUserRepo.Setup(repo => repo.GetUserByUsernameAsync("logintest"))
                        .ReturnsAsync(testUser);

            // IConfiguration'dan JWT ayarlarını mock'la
            var jwtSettings = new Dictionary<string, string?>
            {
                {"Jwt:Issuer", "http://testhost"},
                {"Jwt:Audience", "http://testhost"},
                {"Jwt:Key", "MySuperSecretTestKeyThatIsLongEnoughForSha256"}, // En az 16 byte (UTF8) olmalı
                {"Jwt:TokenValidityInMinutes", "60"}
            };
            // IConfigurationSection mock'ları
            var mockJwtSection = new Mock<IConfigurationSection>();
            mockJwtSection.Setup(s => s.Value).Returns((string?)null); // Section'ın kendisi bir değer döndürmez
            mockJwtSection.Setup(s => s.GetChildren()).Returns(jwtSettings.Select(kvp => {
                var mockChildSection = new Mock<IConfigurationSection>();
                mockChildSection.Setup(cs => cs.Path).Returns($"Jwt:{kvp.Key.Split(':').Last()}"); // Path'i doğru ayarla
                mockChildSection.Setup(cs => cs.Key).Returns(kvp.Key.Split(':').Last());
                mockChildSection.Setup(cs => cs.Value).Returns(kvp.Value);
                return mockChildSection.Object;
            }).ToList());


            // IConfiguration'ın GetSection("Jwt") çağrısını mock'la
             mockConfig.Setup(c => c.GetSection("Jwt")).Returns(mockJwtSection.Object);
             // IConfiguration'ın ["Jwt:Key"] gibi indexer erişimlerini mock'la
             foreach (var kvp in jwtSettings)
             {
                 mockConfig.Setup(c => c[kvp.Key]).Returns(kvp.Value);
             }
             // GetValue<int> mock'u
             mockConfig.Setup(c => c.GetValue<int>("Jwt:TokenValidityInMinutes")).Returns(60);


            var controller = new AuthController(mockUserRepo.Object, mockConfig.Object, mockLogger.Object);
            var loginDto = new UserLoginDto { Username = "logintest", Password = "validpassword" };

            // Act
            var result = await controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
            var returnValue = Assert.IsType<LoginResponseDto>(okResult.Value);
            Assert.NotNull(returnValue.Token);
            Assert.NotEmpty(returnValue.Token);
            Assert.Equal("logintest", returnValue.Username);

            // Token'ı decode edip claim'leri kontrol edebiliriz (daha ileri seviye)
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(returnValue.Token) as JwtSecurityToken;
            Assert.NotNull(jsonToken);
            Assert.Equal(testUser.Username, jsonToken.Claims.First(claim => claim.Type == ClaimTypes.Name).Value);
            Assert.Equal(testUser.Role, jsonToken.Claims.First(claim => claim.Type == ClaimTypes.Role).Value);
        }
    }
}