using DAMH.Models;
using DAMH.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace DAMH.Services.Interfaces
{
    public interface IReadingHistoryService
    {
        Task<bool> RecordReadingAsync(string userId, int bookId, int chapterId);
        Task<IEnumerable<ReadingHistory>> GetUserHistoryAsync(string userId);
        Task<IEnumerable<ReadingHistory>> GetUniqueUserHistoryAsync(string userId);
        Task<ReadingHistory?> GetLastReadingAsync(string userId, int bookId);
        Task<int> GetTotalReadingsAsync();
    }
}
