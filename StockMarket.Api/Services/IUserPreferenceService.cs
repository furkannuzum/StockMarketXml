// StockMarket.Api/Services/IUserPreferenceService.cs
using StockMarket.Api.Models;
using System.Threading.Tasks;
using System.Collections.Generic; // List için
// Microsoft.AspNetCore.JsonPatch.JsonPatchDocument; // PATCH için bu using eklenecek

namespace StockMarket.Api.Services
{
    public interface IUserPreferenceService
    {
        Task<StockUserPreferenceResponseDto?> GetPreferenceAsync(string userId, string tickerSymbol);
        Task<IEnumerable<StockUserPreferenceResponseDto>> GetAllPreferencesForUserAsync(string userId);
        Task<StockUserPreferenceResponseDto?> CreateOrUpdatePreferenceAsync(string userId, string tickerSymbol, StockUserPreferenceCreateUpdateDto dto); // PUT için
        Task<bool> DeletePreferenceAsync(string userId, string tickerSymbol);
        // Task<StockUserPreferenceResponseDto?> PatchPreferenceAsync(string userId, string tickerSymbol, JsonPatchDocument<StockUserPreferenceCreateUpdateDto> patchDoc); // PATCH için
    }
}