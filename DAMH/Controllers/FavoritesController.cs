using DAMH.Data;
using DAMH.Models;
using DAMH.Models.ViewModels;
using DAMH.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DAMH.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 30;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            var (favorites, totalCount) = await _favoriteService.GetPagedFavoritesAsync(page, pageSize, userId);

            var totalPages = totalCount > 0 ? (totalCount + pageSize - 1) / pageSize : 0;

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View(favorites);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int bookId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            var (success, message, isFavorited) = await _favoriteService.ToggleFavoriteAsync(userId, bookId);

            return Json(new
            {
                success = success,
                isFavorited = isFavorited,
                message = message
            });
        }

        [HttpGet]
        public async Task<IActionResult> CheckStatus(int bookId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Json(new { isFavorited = false });

            var isFavorited = await _favoriteService.IsFavoritedAsync(userId, bookId);
            return Json(new { isFavorited = isFavorited });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            await _favoriteService.RemoveFavoriteAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Json(new { count = 0 });

            var count = await _favoriteService.GetUserFavoriteCountAsync(userId);
            return Json(new { count });
        }
    }

}