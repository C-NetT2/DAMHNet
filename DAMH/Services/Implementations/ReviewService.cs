using DAMH.Data;
using DAMH.Models;
using DAMH.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAMH.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly LibraryContext _context;

        public ReviewService(LibraryContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, double AverageRating, int TotalReviews)> SubmitReviewAsync(string userId, int bookId, int rating, string? comment = null)
        {
            try
            {
                if (rating < 1 || rating > 5)
                    return (false, "Đánh giá phải từ 1 đến 5 sao", 0, 0);

                var existingReview = await _context.Reviews.FirstOrDefaultAsync(r => r.BookId == bookId && r.UserId == userId);

                if (existingReview != null)
                {
                    existingReview.Rating = rating;
                    existingReview.Comment = comment;
                    existingReview.UpdatedDate = DateTime.Now;
                }
                else
                {
                    var review = new Review
                    {
                        BookId = bookId,
                        UserId = userId,
                        Rating = rating,
                        Comment = comment,
                        CreatedDate = DateTime.Now
                    };
                    _context.Reviews.Add(review);
                }

                await _context.SaveChangesAsync();

                var averageRating = await _context.Reviews.Where(r => r.BookId == bookId).AverageAsync(r => r.Rating);
                var totalReviews = await _context.Reviews.CountAsync(r => r.BookId == bookId);

                return (true, "Đánh giá thành công!", Math.Round(averageRating, 1), totalReviews);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}", 0, 0);
            }
        }

        public async Task<Review?> GetUserReviewForBookAsync(string userId, int bookId)
        {
            return await _context.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);
        }

        public async Task<IEnumerable<Review>> GetBookReviewsAsync(int bookId, int limit = 10)
        {
            return await _context.Reviews
                .Where(r => r.BookId == bookId && !string.IsNullOrEmpty(r.Comment))
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> UpdateReviewAsync(Review review)
        {
            try
            {
                review.UpdatedDate = DateTime.Now;
                _context.Reviews.Update(review);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteReviewAsync(int reviewId)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(reviewId);
                if (review == null) return false;

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsAsync(int page, int pageSize, string? searchTerm = null, int? rating = null, DateTime? startDate = null, DateTime? endDate = null, string sortBy = "newest")
        {
            var query = _context.Reviews.Include(r => r.Book).Include(r => r.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.Book.Title.Contains(searchTerm) || r.User.Email!.Contains(searchTerm) || (r.Comment != null && r.Comment.Contains(searchTerm)));
            }

            if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
                query = query.Where(r => r.Rating == rating.Value);

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
            {
                var endDateTime = endDate.Value.AddDays(1).AddSeconds(-1);
                query = query.Where(r => r.CreatedDate <= endDateTime);
            }

            query = sortBy switch
            {
                "oldest" => query.OrderBy(r => r.CreatedDate),
                "rating_high" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedDate),
                "rating_low" => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedDate),
                _ => query.OrderByDescending(r => r.CreatedDate)
            };

            var totalCount = await query.CountAsync();

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (reviews, totalCount);
        }
    }
}
