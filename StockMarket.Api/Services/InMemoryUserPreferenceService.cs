// StockMarket.Api/Services/InMemoryUserPreferenceService.cs
using StockMarket.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StockMarket.Api.Services;
// using Microsoft.AspNetCore.JsonPatch; // PATCH implementasyonu için eklenecek
// using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // PATCH implementasyonu için eklenecek

namespace StockMarket.Api.Services
{
    public class InMemoryUserPreferenceService : IUserPreferenceService
    {
        // Bellekte tutulacak tercihler listesi.
        // Bu listeyi static yapmak, servisin Scoped veya Transient olması durumunda bile
        // verilerin uygulama boyunca korunmasını sağlar. Eğer servis Singleton ise static olmasına gerek yok.
        // Şimdilik basitlik için static yapalım.
        private static readonly List<StockUserPreference> _preferences = new List<StockUserPreference>();
        private readonly ILogger<InMemoryUserPreferenceService> _logger;
        // private readonly IObjectValidator _objectValidator; // PATCH için validasyon

        public InMemoryUserPreferenceService(ILogger<InMemoryUserPreferenceService> logger) // IObjectValidator objectValidator) // PATCH için
        {
            _logger = logger;
            // _objectValidator = objectValidator; // PATCH için
        }

        private StockUserPreferenceResponseDto MapToResponseDto(StockUserPreference pref)
        {
            return new StockUserPreferenceResponseDto
            {
                TickerSymbol = pref.TickerSymbol,
                Notes = pref.Notes,
                PriceAlertUpper = pref.PriceAlertUpper,
                PriceAlertLower = pref.PriceAlertLower,
                LastModifiedUtc = pref.LastModifiedUtc
            };
        }

        public Task<StockUserPreferenceResponseDto?> GetPreferenceAsync(string userId, string tickerSymbol)
        {
            var preference = _preferences.FirstOrDefault(p =>
                p.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) &&
                p.TickerSymbol.Equals(tickerSymbol, StringComparison.OrdinalIgnoreCase));

            if (preference == null)
            {
                _logger.LogInformation("No preference found for User: {UserId}, Ticker: {TickerSymbol}", userId, tickerSymbol);
                return Task.FromResult<StockUserPreferenceResponseDto?>(null);
            }

            _logger.LogInformation("Preference found for User: {UserId}, Ticker: {TickerSymbol}", userId, tickerSymbol);
            return Task.FromResult<StockUserPreferenceResponseDto?>(MapToResponseDto(preference));
        }

        public Task<IEnumerable<StockUserPreferenceResponseDto>> GetAllPreferencesForUserAsync(string userId)
        {
            var userPreferences = _preferences
                .Where(p => p.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
                .Select(p => MapToResponseDto(p))
                .ToList();

            _logger.LogInformation("Found {Count} preferences for User: {UserId}", userPreferences.Count, userId);
            return Task.FromResult<IEnumerable<StockUserPreferenceResponseDto>>(userPreferences);
        }

        public Task<StockUserPreferenceResponseDto?> CreateOrUpdatePreferenceAsync(string userId, string tickerSymbol, StockUserPreferenceCreateUpdateDto dto) // PUT için
        {
            var symbolUpper = tickerSymbol.ToUpperInvariant();
            var existingPreference = _preferences.FirstOrDefault(p =>
                p.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) &&
                p.TickerSymbol.Equals(symbolUpper, StringComparison.OrdinalIgnoreCase));

            if (existingPreference != null)
            {
                // Güncelleme (PUT: tüm alanlar DTO'dan alınır)
                existingPreference.Notes = dto.Notes;
                existingPreference.PriceAlertUpper = dto.PriceAlertUpper;
                existingPreference.PriceAlertLower = dto.PriceAlertLower;
                existingPreference.LastModifiedUtc = DateTime.UtcNow;
                _logger.LogInformation("Updated preference for User: {UserId}, Ticker: {TickerSymbol}", userId, symbolUpper);
                return Task.FromResult<StockUserPreferenceResponseDto?>(MapToResponseDto(existingPreference));
            }
            else
            {
                // Oluşturma
                var newPreference = new StockUserPreference
                {
                    UserId = userId,
                    TickerSymbol = symbolUpper,
                    Notes = dto.Notes,
                    PriceAlertUpper = dto.PriceAlertUpper,
                    PriceAlertLower = dto.PriceAlertLower,
                    LastModifiedUtc = DateTime.UtcNow
                };
                _preferences.Add(newPreference);
                _logger.LogInformation("Created new preference for User: {UserId}, Ticker: {TickerSymbol}", userId, symbolUpper);
                return Task.FromResult<StockUserPreferenceResponseDto?>(MapToResponseDto(newPreference));
            }
        }

        public Task<bool> DeletePreferenceAsync(string userId, string tickerSymbol)
        {
            var symbolUpper = tickerSymbol.ToUpperInvariant();
            var preferenceToRemove = _preferences.FirstOrDefault(p =>
                p.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) &&
                p.TickerSymbol.Equals(symbolUpper, StringComparison.OrdinalIgnoreCase));

            if (preferenceToRemove != null)
            {
                _preferences.Remove(preferenceToRemove);
                _logger.LogInformation("Deleted preference for User: {UserId}, Ticker: {TickerSymbol}", userId, symbolUpper);
                return Task.FromResult(true);
            }

            _logger.LogWarning("Attempted to delete non-existing preference for User: {UserId}, Ticker: {TickerSymbol}", userId, symbolUpper);
            return Task.FromResult(false);
        }

        // PATCH İÇİN IMPLEMENTASYON (DAHA SONRA EKLENECEK)
        /*
        public Task<StockUserPreferenceResponseDto?> PatchPreferenceAsync(
            string userId,
            string tickerSymbol,
            JsonPatchDocument<StockUserPreferenceCreateUpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                _logger.LogWarning("PatchPreferenceAsync called with null patchDoc for User: {UserId}, Ticker: {TickerSymbol}", userId, tickerSymbol);
                return Task.FromResult<StockUserPreferenceResponseDto?>(null); // Veya BadRequest durumu controller'da ele alınır
            }

            var symbolUpper = tickerSymbol.ToUpperInvariant();
            var existingPreference = _preferences.FirstOrDefault(p =>
                p.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) &&
                p.TickerSymbol.Equals(symbolUpper, StringComparison.OrdinalIgnoreCase));

            if (existingPreference == null)
            {
                _logger.LogWarning("PatchPreferenceAsync: No preference found to patch for User: {UserId}, Ticker: {TickerSymbol}", userId, symbolUpper);
                return Task.FromResult<StockUserPreferenceResponseDto?>(null); // Controller NotFound dönebilir
            }

            // Mevcut veriyi DTO'ya map et, patch'i uygula, sonra DTO'yu tekrar modele map et
            var preferenceToPatchDto = new StockUserPreferenceCreateUpdateDto
            {
                Notes = existingPreference.Notes,
                PriceAlertUpper = existingPreference.PriceAlertUpper,
                PriceAlertLower = existingPreference.PriceAlertLower
            };

            // JsonPatchDocument'u uygula.
            // Bu işlem ModelState hataları üretebilir, bunları yakalamak ve controller'a bildirmek gerekir.
            // patchDoc.ApplyTo(preferenceToPatchDto, _objectValidator); // ModelState'i kontrol etmek için _objectValidator gerekir
            patchDoc.ApplyTo(preferenceToPatchDto); // Basit uygulama, validasyon controller'da yapılmalı

            // Validasyon (eğer DTO'da DataAnnotations varsa ve _objectValidator kullanılıyorsa)
            // if (!_objectValidator.IsValid(preferenceToPatchDto))
            // {
            //     // Hataları logla veya controller'a ilet
            //     _logger.LogWarning("Patch resulted in invalid DTO for User: {UserId}, Ticker: {TickerSymbol}", userId, symbolUpper);
            //     return Task.FromResult<StockUserPreferenceResponseDto?>(null); // Controller BadRequest dönebilir
            // }


            // DTO'dan gelen güncellenmiş değerleri ana modele aktar
            existingPreference.Notes = preferenceToPatchDto.Notes;
            existingPreference.PriceAlertUpper = preferenceToPatchDto.PriceAlertUpper;
            existingPreference.PriceAlertLower = preferenceToPatchDto.PriceAlertLower;
            existingPreference.LastModifiedUtc = DateTime.UtcNow;

            _logger.LogInformation("Patched preference for User: {UserId}, Ticker: {TickerSymbol}", userId, symbolUpper);
            return Task.FromResult<StockUserPreferenceResponseDto?>(MapToResponseDto(existingPreference));
        }
        */
    }
}