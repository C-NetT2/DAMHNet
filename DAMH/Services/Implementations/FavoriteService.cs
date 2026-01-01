using DAMH.Data;
using DAMH.Models;
using DAMH.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAMH.Services.Implementations
{
    public class FavoriteService : IFavoriteService
    {
        private readonly LibraryContext _context;

        public FavoriteService(LibraryContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, bool IsFavorited)> ToggleFavoriteAsync(string userId, int bookId)
        {
            try
            {
                var existingFav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);

                bool isFavorited;
                if (existingFav != null)
                {
                    _context.Favorites.Remove(existingFav);
                    isFavorited = false;
                }
                else
                {
                    var newFav = new Favorite { UserId = userId, BookId = bookId, DateAdded = DateTime.Now };
                    _context.Favorites.Add(newFav);
                    isFavorited = true;
                }

                await _context.SaveChangesAsync();

                var message = isFavorited ? "Đã thêm vào yêu thích!" : "Đã xóa khỏi yêu thích!";
                return (true, message, isFavorited);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}", false);
            }
        }

        public async Task<bool> IsFavoritedAsync(string userId, int bookId)
        {
            return await _context.Favorites.AnyAsync(f => f.UserId == userId && f.BookId == bookId);
        }

        public async Task<IEnumerable<Favorite>> GetUserFavoritesAsync(string userId, int page, int pageSize)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Book)
                .OrderByDescending(f => f.DateAdded)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUserFavoriteCountAsync(string userId)
        {
            return await _context.Favorites.Where(f => f.UserId == userId).CountAsync();
        }

        public async Task<bool> RemoveFavoriteAsync(int favoriteId, string userId)
        {
            try
            {
                var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.FavoriteId == favoriteId && f.UserId == userId);

                if (fav == null) return false;

                _context.Favorites.Remove(fav);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(IEnumerable<Favorite> Favorites, int TotalCount)> GetPagedFavoritesAsync(int page, int pageSize, string? userId = null, int? bookId = null)
        {
            var query = _context.Favorites.Include(f => f.User).Include(f => f.Book).AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(f => f.UserId == userId);

            if (bookId.HasValue)
                query = query.Where(f => f.BookId == bookId);

            var totalCount = await query.CountAsync();

            var favorites = await query
                .OrderByDescending(f => f.DateAdded)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (favorites, totalCount);
        }
    }

}