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
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] ReviewViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            var (success, message, averageRating, totalReviews) = await _reviewService.SubmitReviewAsync(
                userId, model.BookId, model.Rating, model.Comment);

            return Json(new
            {
                success = success,
                message = message,
                averageRating = averageRating,
                totalReviews = totalReviews
            });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitComment([FromBody] dynamic request)
        {
            try
            {
                int bookId = request.bookId;
                string comment = request.comment;

                if (string.IsNullOrWhiteSpace(comment) || comment.Length > 500)
                    return Json(new { success = false, message = "Bình luận không hợp lệ" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });

                var (success, message, _, _) = await _reviewService.SubmitReviewAsync(userId, bookId, 0, comment);

                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(int bookId)
        {
            var reviews = await _reviewService.GetBookReviewsAsync(bookId, 5);

            var comments = reviews.Select(r => new
            {
                r.ReviewId,
                userName = r.User.FullName ?? r.User.Email,
                content = r.Comment,
                createdDate = r.CreatedDate
            });

            return Json(comments);
        }
    }
}