using DAMH.Models;
using DAMH.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace DAMH.Services.Interfaces
{
    public interface IReviewService
    {
        Task<(bool Success, string Message, double AverageRating, int TotalReviews)> SubmitReviewAsync(string userId, int bookId, int rating, string? comment = null);
        Task<Review?> GetUserReviewForBookAsync(string userId, int bookId);
        Task<IEnumerable<Review>> GetBookReviewsAsync(int bookId, int limit = 10);
        Task<bool> UpdateReviewAsync(Review review);
        Task<bool> DeleteReviewAsync(int reviewId);
        Task<(IEnumerable<Review> Reviews, int TotalCount)> GetPagedReviewsAsync(int page, int pageSize, string? searchTerm = null, int? rating = null, DateTime? startDate = null, DateTime? endDate = null, string sortBy = "newest");
    }
}
