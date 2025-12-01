using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DAMH.Data;
using DAMH.Models;
using System.Security.Claims;

namespace DAMH.Controllers
{
    [Authorize] // Phải đăng nhập
    public class FavoritesController : Controller
    {
        private readonly LibraryContext _context;

        public FavoritesController(LibraryContext context)
        {
            _context = context;
        }

        // 1. TRANG DANH SÁCH YÊU THÍCH
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Book)
                .OrderByDescending(f => f.DateAdded)
                .ToListAsync();

            return View(favorites);
        }

        // 2. TOGGLE YÊU THÍCH (AJAX)
        [HttpPost]
        public async Task<IActionResult> Toggle(int bookId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var existingFav = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);

            bool isFavorited;

            if (existingFav != null)
            {
                _context.Favorites.Remove(existingFav);
                isFavorited = false;
            }
            else
            {
                var newFav = new Favorite { UserId = userId, BookId = bookId };
                _context.Favorites.Add(newFav);
                isFavorited = true;
            }

            await _context.SaveChangesAsync();
            return Json(new
            {
                success = true,
                isFavorited = isFavorited,
                message = isFavorited ? "Đã thêm vào yêu thích!" : "Đã xóa khỏi yêu thích!"
            });
        }

        // 3. KIỂM TRA TRẠNG THÁI (AJAX)
        [HttpGet]
        public async Task<IActionResult> CheckStatus(int bookId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Json(new { isFavorited = false });

            var isFavorited = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.BookId == bookId);

            return Json(new { isFavorited = isFavorited });
        }

        // 4. XÓA KHỎI DANH SÁCH (Nút xóa trực tiếp)
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.FavoriteId == id && f.UserId == userId);

            if (fav != null)
            {
                _context.Favorites.Remove(fav);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}