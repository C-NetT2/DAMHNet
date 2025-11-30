using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAMH.Data;
using DAMH.Models;
using DAMH.Models.ViewModels;

namespace DAMH.Controllers
{
    [Authorize] // Phải đăng nhập mới được vote
    public class ReviewController : Controller
    {
        private readonly LibraryContext _context;

        public ReviewController(LibraryContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] ReviewViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookId == model.BookId && r.UserId == userId);

            if (existingReview != null)
            {
                existingReview.Rating = model.Rating;
                existingReview.Comment = model.Comment;
                existingReview.UpdatedDate = DateTime.Now;
            }
            else
            {
                var review = new Review
                {
                    BookId = model.BookId,
                    UserId = userId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    CreatedDate = DateTime.Now
                };
                _context.Reviews.Add(review);
            }

            await _context.SaveChangesAsync();

            // Tính lại điểm trung bình để trả về giao diện
            var averageRating = await _context.Reviews.Where(r => r.BookId == model.BookId).AverageAsync(r => r.Rating);
            var totalReviews = await _context.Reviews.CountAsync(r => r.BookId == model.BookId);

            return Json(new
            {
                success = true,
                message = "Đánh giá thành công!",
                averageRating = Math.Round(averageRating, 1),
                totalReviews = totalReviews
            });
        }
    }
}