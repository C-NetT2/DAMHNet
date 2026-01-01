using DAMH.Models;
using DAMH.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace DAMH.Services.Interfaces
{
    public interface IFavoriteService
    {
        Task<(bool Success, string Message, bool IsFavorited)> ToggleFavoriteAsync(string userId, int bookId);
        Task<bool> IsFavoritedAsync(string userId, int bookId);
        Task<IEnumerable<Favorite>> GetUserFavoritesAsync(string userId, int page, int pageSize);
        Task<int> GetUserFavoriteCountAsync(string userId);
        Task<bool> RemoveFavoriteAsync(int favoriteId, string userId);
        Task<(IEnumerable<Favorite> Favorites, int TotalCount)> GetPagedFavoritesAsync(int page, int pageSize, string? userId = null, int? bookId = null);
    }
}
