using DAMH.Data;
using DAMH.Models;
using DAMH.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAMH.Services.Implementations
{
    public class ReadingHistoryService : IReadingHistoryService
    {
        private readonly LibraryContext _context;

        public ReadingHistoryService(LibraryContext context)
        {
            _context = context;
        }

        public async Task<bool> RecordReadingAsync(string userId, int bookId, int chapterId)
        {
            try
            {
                var existingHistory = await _context.ReadingHistories.FirstOrDefaultAsync(rh => rh.UserId == userId && rh.BookId == bookId);
                if (existingHistory != null)
                {
                    existingHistory.ChapterId = chapterId;
                    existingHistory.AccessTime = DateTime.Now;
                    _context.Update(existingHistory);
                }
                else
                {
                    _context.ReadingHistories.Add(new ReadingHistory { UserId = userId, BookId = bookId, ChapterId = chapterId, AccessTime = DateTime.Now });
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<ReadingHistory>> GetUserHistoryAsync(string userId)
        {
            return await _context.ReadingHistories.Where(rh => rh.UserId == userId).Include(rh => rh.Book).Include(rh => rh.Chapter).OrderByDescending(rh => rh.AccessTime).ToListAsync();
        }

        public async Task<IEnumerable<ReadingHistory>> GetUniqueUserHistoryAsync(string userId)
        {
            var rawHistory = await GetUserHistoryAsync(userId);
            return rawHistory.GroupBy(rh => rh.BookId).Select(g => g.First()).ToList();
        }

        public async Task<ReadingHistory?> GetLastReadingAsync(string userId, int bookId)
        {
            return await _context.ReadingHistories.Where(rh => rh.UserId == userId && rh.BookId == bookId).Include(rh => rh.Chapter).OrderByDescending(rh => rh.AccessTime).FirstOrDefaultAsync();
        }

        public async Task<int> GetTotalReadingsAsync()
        {
            return await _context.ReadingHistories.CountAsync();
        }
    }
}
