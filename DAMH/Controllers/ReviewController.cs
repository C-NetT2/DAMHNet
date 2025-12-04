using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAMH.Data;
using DAMH.Models;
using DAMH.Models.ViewModels;

namespace DAMH.Controllers
{
    [Authorize] 
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

        [HttpPost]
        public async Task<IActionResult> SubmitComment([FromBody] dynamic request)
        {
            try
            {
                int bookId = request.bookId;
                string comment = request.comment;

                if (string.IsNullOrWhiteSpace(comment) || comment.Length > 500)
                {
                    return Json(new { success = false, message = "Bình luận không hợp lệ" });
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });

                var review = new Review
                {
                    BookId = bookId,
                    UserId = userId,
                    Comment = comment,
                    CreatedDate = DateTime.Now,
                    Rating = 0  // Bình luận không có rating
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Bình luận đã được gửi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(int bookId)
        {
            var comments = await _context.Reviews
                .Where(r => r.BookId == bookId && !string.IsNullOrEmpty(r.Comment))
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .Take(5)
                .Select(r => new
                {
                    r.ReviewId,
                    userName = r.User.FullName ?? r.User.Email,
                    content = r.Comment,
                    createdDate = r.CreatedDate
                })
                .ToListAsync();

            return Json(comments);
        }
    }
}