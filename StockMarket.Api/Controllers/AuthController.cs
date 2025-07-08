// StockMarket.Api/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StockMarket.Api.Models;
using StockMarket.Api.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration; // IConfiguration için
using BCrypt.Net; // BCrypt için

namespace StockMarket.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [Produces("application/json")] // Auth endpoint'leri genellikle JSON döner, XML değil.
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserRepository userRepository, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
        }

        // POST: api/v1/auth/register
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existingUser = await _userRepository.GetUserByUsernameAsync(registerDto.Username);
                if (existingUser != null)
                {
                    _logger.LogWarning("Kullanıcı adı '{Username}' zaten mevcut.", registerDto.Username);
                    return BadRequest(new { message = $"Kullanıcı adı '{registerDto.Username}' zaten kullanılıyor." });
                }

                // Şifreyi hash'le
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                var newUser = new User
                {
                    Username = registerDto.Username,
                    PasswordHash = passwordHash,
                    Role = "User" // Varsayılan rol
                };

                await _userRepository.AddUserAsync(newUser);
                _logger.LogInformation("Kullanıcı '{Username}' başarıyla kaydedildi.", newUser.Username);

                // Kayıt sonrası ne döneceğimize karar verebiliriz.
                // Genellikle 201 Created veya kullanıcı bilgileri (şifresiz) dönebilir.
                // Şimdilik sadece 201 dönelim.
                return CreatedAtAction(nameof(Login), new { username = newUser.Username }, new { message = "Kullanıcı başarıyla oluşturuldu." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register endpoint'inde hata oluştu.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Kayıt sırasında bir hata oluştu." });
            }
        }

        // POST: api/v1/auth/login
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
       public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Kullanıcı adı veya şifre hatalı: {Username}", loginDto.Username);
                    return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı." });
                }

                var token = GenerateJwtToken(user);
                // === LOGLAMA GÜNCELLENDİ: Rol bilgisini de logla ===
                _logger.LogInformation("Kullanıcı '{Username}' başarıyla giriş yaptı. Rol: {Role}", user.Username, user.Role);

                // === YANIT GÜNCELLENDİ: LoginResponseDto'ya Rol eklendi ===
                return Ok(new LoginResponseDto
                {
                    Token = token,
                    Username = user.Username,
                    Role = user.Role // <-- EKSİK OLAN KISIM BUYDU! user nesnesinden rolü alıp DTO'ya ekliyoruz.
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login endpoint'inde hata oluştu.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Giriş sırasında bir hata oluştu." });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var tokenValidityInMinutes = _configuration.GetValue<int>("Jwt:TokenValidityInMinutes");

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                _logger.LogError("JWT ayarları (Key, Issuer, Audience) appsettings.json dosyasında eksik veya hatalı.");
                throw new InvalidOperationException("JWT ayarları yapılandırılmamış.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // Subject (kullanıcı ID'si)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID (her token için benzersiz)
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64), // Issued at
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ASP.NET Core'un User.Identity.NameIdentifier'ı için
                new Claim(ClaimTypes.Name, user.Username), // ASP.NET Core'un User.Identity.Name'i için
                new Claim(ClaimTypes.Role, user.Role) // Kullanıcının rolü
                // İsteğe bağlı olarak başka claim'ler eklenebilir
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(tokenValidityInMinutes),
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}