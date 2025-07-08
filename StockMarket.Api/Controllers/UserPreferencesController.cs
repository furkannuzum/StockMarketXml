// StockMarket.Api/Controllers/UserPreferencesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch; // PATCH için bu using gerekli olacak
using Microsoft.AspNetCore.Mvc;
using StockMarket.Api.Models;
using StockMarket.Api.Services;
using System;
using System.Linq;
using System.Security.Claims; // UserId'yi Claim'lerden almak için
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt; // <-- BU SATIRI EKLE
namespace StockMarket.Api.Controllers
{
    [ApiController]
    [Route("api/v1/user/preferences")] // Kullanıcıya özel olduğu için /user/preferences gibi bir path
    [Authorize] // Bu controller'daki tüm endpoint'ler kimlik doğrulaması gerektirir
    [Produces("application/json")] // Bu controller genellikle JSON dönecek
    public class UserPreferencesController : ControllerBase
    {
        private readonly IUserPreferenceService _preferenceService;
        private readonly ILogger<UserPreferencesController> _logger;

        public UserPreferencesController(IUserPreferenceService preferenceService, ILogger<UserPreferencesController> logger)
        {
            _preferenceService = preferenceService;
            _logger = logger;
        }

        private string? GetCurrentUserId()
        {
            // JWT'den kullanıcı ID'sini al (Subject veya NameIdentifier claim'i)
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }

        // GET: api/v1/user/preferences
        /// <summary>
        /// Gets all preferences for the authenticated user.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<StockUserPreferenceResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllUserPreferences()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID could not be determined from token." });
            }

            _logger.LogInformation("User {UserId} fetching all their preferences.", userId);
            var preferences = await _preferenceService.GetAllPreferencesForUserAsync(userId);
            return Ok(preferences);
        }


        // GET: api/v1/user/preferences/{tickerSymbol}
        /// <summary>
        /// Gets the preference for a specific stock for the authenticated user.
        /// </summary>
        /// <param name="tickerSymbol">The stock ticker symbol.</param>
        [HttpGet("{tickerSymbol}")]
        [ProducesResponseType(typeof(StockUserPreferenceResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPreferenceForTicker(string tickerSymbol)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID could not be determined from token." });
            }
            if (string.IsNullOrWhiteSpace(tickerSymbol))
            {
                return BadRequest(new { message = "Ticker symbol cannot be empty." });
            }

            _logger.LogInformation("User {UserId} fetching preference for ticker: {TickerSymbol}", userId, tickerSymbol);
            var preference = await _preferenceService.GetPreferenceAsync(userId, tickerSymbol);

            if (preference == null)
            {
                return NotFound(new { message = $"No preference found for ticker '{tickerSymbol}' for the current user." });
            }
            return Ok(preference);
        }

        // PUT: api/v1/user/preferences/{tickerSymbol}
        /// <summary>
        /// Creates or updates the preference for a specific stock for the authenticated user.
        /// </summary>
        /// <param name="tickerSymbol">The stock ticker symbol.</param>
        /// <param name="preferenceDto">The preference data.</param>
        [HttpPut("{tickerSymbol}")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(StockUserPreferenceResponseDto), StatusCodes.Status200OK)] // Güncelleme başarılı
        [ProducesResponseType(typeof(StockUserPreferenceResponseDto), StatusCodes.Status201Created)] // Oluşturma başarılı
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateOrUpdatePreference(string tickerSymbol, [FromBody] StockUserPreferenceCreateUpdateDto preferenceDto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID could not be determined from token." });
            }
            if (string.IsNullOrWhiteSpace(tickerSymbol))
            {
                return BadRequest(new { message = "Ticker symbol cannot be empty." });
            }
            if (!ModelState.IsValid) // DTO validasyonu
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("User {UserId} creating/updating preference for ticker: {TickerSymbol}", userId, tickerSymbol);
            // Servis metodu hem oluşturma hem de güncelleme yapacak.
            // Var olup olmadığını kontrol edip ona göre 200 OK veya 201 Created dönebiliriz.
            var existingPreference = await _preferenceService.GetPreferenceAsync(userId, tickerSymbol);
            var result = await _preferenceService.CreateOrUpdatePreferenceAsync(userId, tickerSymbol, preferenceDto);

            if (result == null) // Bu normalde olmamalı, servis bir DTO dönmeli
            {
                 _logger.LogError("CreateOrUpdatePreferenceAsync returned null for User: {UserId}, Ticker: {TickerSymbol}", userId, tickerSymbol);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while saving preference." });
            }

            if (existingPreference != null) // Güncellendi
            {
                return Ok(result);
            }
            else // Yeni oluşturuldu
            {
                // Oluşturulan kaynağın URI'sini döndürmek iyi bir pratiktir.
                return CreatedAtAction(nameof(GetPreferenceForTicker), new { tickerSymbol = result.TickerSymbol }, result);
            }
        }

        // DELETE: api/v1/user/preferences/{tickerSymbol}
        /// <summary>
        /// Deletes the preference for a specific stock for the authenticated user.
        /// </summary>
        /// <param name="tickerSymbol">The stock ticker symbol.</param>
        [HttpDelete("{tickerSymbol}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Başarılı silme
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePreference(string tickerSymbol)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID could not be determined from token." });
            }
            if (string.IsNullOrWhiteSpace(tickerSymbol))
            {
                return BadRequest(new { message = "Ticker symbol cannot be empty." });
            }

            _logger.LogInformation("User {UserId} deleting preference for ticker: {TickerSymbol}", userId, tickerSymbol);
            var success = await _preferenceService.DeletePreferenceAsync(userId, tickerSymbol);

            if (!success)
            {
                return NotFound(new { message = $"No preference found to delete for ticker '{tickerSymbol}' for the current user." });
            }
            return NoContent(); // Silme başarılı, içerik yok.
        }


        // // PATCH: api/v1/user/preferences/{tickerSymbol} (İSTEĞE BAĞLI - DAHA İLERİ SEVİYE)
        // /// <summary>
        // /// Partially updates the preference for a specific stock for the authenticated user.
        // /// Requires 'Content-Type: application/json-patch+json'.
        // /// </summary>
        // /// <param name="tickerSymbol">The stock ticker symbol.</param>
        // /// <param name="patchDoc">The JSON Patch document describing the updates.</param>
        // /*  // PATCH implementasyonu için aşağıdaki using'ler ve paketler gerekir:
        //     // NuGet: Microsoft.AspNetCore.JsonPatch
        //     // NuGet: Microsoft.AspNetCore.Mvc.NewtonsoftJson (Eğer Newtonsoft.Json tabanlı JsonPatch kullanılıyorsa)
        //     // using Microsoft.AspNetCore.JsonPatch;

        // [HttpPatch("{tickerSymbol}")]
        // [Consumes("application/json-patch+json")] // PATCH için özel içerik tipi
        // [ProducesResponseType(typeof(StockUserPreferenceResponseDto), StatusCodes.Status200OK)]
        // [ProducesResponseType(StatusCodes.Status400BadRequest)]
        // [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        // [ProducesResponseType(StatusCodes.Status404NotFound)]
        // public async Task<IActionResult> PatchPreference(string tickerSymbol,
        //     [FromBody] JsonPatchDocument<StockUserPreferenceCreateUpdateDto>? patchDoc)
        // {
        //     var userId = GetCurrentUserId();
        //     if (string.IsNullOrEmpty(userId))
        //     {
        //         return Unauthorized(new { message = "User ID could not be determined from token." });
        //     }
        //     if (string.IsNullOrWhiteSpace(tickerSymbol))
        //     {
        //         return BadRequest(new { message = "Ticker symbol cannot be empty." });
        //     }
        //     if (patchDoc == null)
        //     {
        //         return BadRequest(new { message = "A JSON Patch document is required." });
        //     }

        //     _logger.LogInformation("User {UserId} patching preference for ticker: {TickerSymbol}", userId, tickerSymbol);

        //     var existingPreferenceModel = await _preferenceService.GetRawPreferenceAsync(userId, tickerSymbol); // Serviste bu metot lazım olacak (StockUserPreference dönecek)
        //     if (existingPreferenceModel == null)
        //     {
        //         return NotFound(new { message = $"No preference found to patch for ticker '{tickerSymbol}' for the current user." });
        //     }

        //     // Mevcut modeli DTO'ya çevir, patch'i uygula, validasyonu yap, sonra DTO'yu modele geri çevir
        //     var preferenceToPatchDto = new StockUserPreferenceCreateUpdateDto
        //     {
        //         Notes = existingPreferenceModel.Notes,
        //         PriceAlertLower = existingPreferenceModel.PriceAlertLower,
        //         PriceAlertUpper = existingPreferenceModel.PriceAlertUpper
        //     };

        //     patchDoc.ApplyTo(preferenceToPatchDto, ModelState); // ModelState'e hataları ekler

        //     // ModelState.IsValid, DataAnnotations ve JsonPatchDocument'un uyguladığı değişikliklerin
        //     // DTO'daki validasyon kurallarına (örn: Range) uyup uymadığını kontrol eder.
        //     if (!TryValidateModel(preferenceToPatchDto)) // JsonPatch sonrası DTO'yu tekrar valide et
        //     {
        //         return BadRequest(ModelState);
        //     }

        //     // Servise güncellenmiş DTO'yu gönder
        //     var updatedPreference = await _preferenceService.CreateOrUpdatePreferenceAsync(userId, tickerSymbol, preferenceToPatchDto);

        //     if (updatedPreference == null)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while patching preference." });
        //     }

        //     return Ok(updatedPreference);
        // }
        // */
        // // PATCH için IUserPreferenceService'e GetRawPreferenceAsync(string userId, string tickerSymbol) gibi bir metot eklemek gerekebilir
        // // Bu metot, patch uygulanacak ham StockUserPreference nesnesini döndürür.
        // // Veya PatchPreferenceAsync metodu doğrudan serviste de implemente edilebilir.
    }
}