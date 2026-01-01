using DAMH.Models;

namespace DAMH.Services.Interfaces
{
    public interface IBookService
    {
        Task<IEnumerable<Book>> GetAllBooksAsync();
        Task<Book?> GetBookByIdAsync(int id);
        Task<Book?> GetBookWithChaptersAsync(int id);
        Task<Book?> GetBookWithDetailsAsync(int id);
        Task<bool> CreateBookAsync(Book book);
        Task<bool> UpdateBookAsync(Book book);
        Task<bool> DeleteBookAsync(int id);

        Task<IEnumerable<Book>> SearchBooksAsync(string keyword, BookType? bookType, Genre? genre, AgeRating? ageRating);
        Task<(IEnumerable<Book> Books, int TotalCount)> GetPagedBooksAsync(int page, int pageSize, string searchTerm = "");

        Task<IEnumerable<Book>> GetHotBooksAsync(int count = 10);
        Task<IEnumerable<Book>> GetNewUpdatesAsync(int count = 15);
        Task<IEnumerable<Book>> GetTopRatedBooksAsync(int count = 10);
        Task<IEnumerable<Book>> GetMostViewedBooksAsync(int count = 10);
        Task<IEnumerable<Book>> GetMostFavoritedBooksAsync(int count = 10);

        Task IncrementViewCountAsync(int bookId);
        Task<bool> UpdateCoverImageAsync(int bookId, string coverUrl);
        Task<bool> BookExistsAsync(int id);
    }
}