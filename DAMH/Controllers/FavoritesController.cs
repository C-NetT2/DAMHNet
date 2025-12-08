using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DAMH.Data;
using DAMH.Models;
using System.Security.Claims;

namespace DAMH.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly LibraryContext _context;

        public FavoritesController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 30;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var query = _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Book)
                .OrderByDescending(f => f.DateAdded);

            var totalCount = await query.CountAsync();

            var totalPages = totalCount > 0 ? (totalCount + pageSize - 1) / pageSize : 0;

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var favorites = totalCount > 0
                ? await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync()
                : new List<Favorite>();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(favorites);
        }

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

        [HttpGet]
        public async Task<IActionResult> CheckStatus(int bookId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Json(new { isFavorited = false });

            var isFavorited = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.BookId == bookId);

            return Json(new { isFavorited = isFavorited });
        }

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

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Json(new { count = 0 });

            var count = await _context.Favorites
                .Where(f => f.UserId == userId)
                .CountAsync();

            return Json(new { count });
        }
    }
}