using DAMH.Data;
using DAMH.Models;
using DAMH.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace DAMH.Services.Implementations
{
    public class BookService : IBookService
    {
        private readonly LibraryContext _context;

        public BookService(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Book>> GetAllBooksAsync()
        {
            return await _context.Books
                .Include(b => b.Reviews)
                .Include(b => b.Chapters)
                .OrderByDescending(b => b.LastUpdated)
                .ToListAsync();
        }

        public async Task<Book?> GetBookByIdAsync(int id)
        {
            return await _context.Books
                .Include(b => b.Reviews)
                .Include(b => b.Chapters)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public async Task<Book?> GetBookWithChaptersAsync(int id)
        {
            return await _context.Books
                .Include(b => b.Chapters.OrderBy(c => c.ChapterOrder))
                .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public async Task<Book?> GetBookWithDetailsAsync(int id)
        {
            return await _context.Books
                .Include(b => b.Reviews).ThenInclude(r => r.User)
                .Include(b => b.Chapters.OrderBy(c => c.ChapterOrder))
                .Include(b => b.MediaFiles)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public async Task<bool> CreateBookAsync(Book book)
        {
            try
            {
                book.CreatedDate = DateTime.Now;
                book.LastUpdated = DateTime.Now;
                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateBookAsync(Book book)
        {
            try
            {
                book.LastUpdated = DateTime.Now;
                _context.Books.Update(book);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteBookAsync(int id)
        {
            try
            {
                var book = await _context.Books.FindAsync(id);
                if (book == null) return false;

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<Book>> SearchBooksAsync(string keyword, BookType? bookType, Genre? genre, AgeRating? ageRating)
        {
            var query = _context.Books
                .Include(b => b.Reviews)
                .Include(b => b.Chapters)
                .AsQueryable();

            if (bookType.HasValue)
                query = query.Where(b => b.BookType == bookType.Value);

            if (genre.HasValue)
                query = query.Where(b => b.Genre == genre.Value);

            if (ageRating.HasValue)
                query = query.Where(b => b.AgeRating == ageRating.Value);

            var allBooks = await query.ToListAsync();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string searchStr = RemoveDiacritics(keyword.ToLower());
                var tokens = searchStr.Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);

                var resultBooks = new List<Book>();
                foreach (var book in allBooks)
                {
                    string titleNorm = RemoveDiacritics((book.Title ?? "").ToLower());
                    string authorNorm = RemoveDiacritics((book.Author ?? "").ToLower());

                    int matchCount = tokens.Count(token => titleNorm.Contains(token) || authorNorm.Contains(token));

                    if (matchCount >= (tokens.Length * 0.5))
                    {
                        resultBooks.Add(book);
                    }
                }
                return resultBooks;
            }

            return allBooks;
        }

        public async Task<(IEnumerable<Book> Books, int TotalCount)> GetPagedBooksAsync(int page, int pageSize, string searchTerm = "")
        {
            var query = _context.Books.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(b => b.Title.Contains(searchTerm) ||
                                        (b.Author != null && b.Author.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var books = await query
                .Include(b => b.Reviews)
                .Include(b => b.Chapters)
                .OrderByDescending(b => b.LastUpdated)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (books, totalCount);
        }

        public async Task<IEnumerable<Book>> GetHotBooksAsync(int count = 10)
        {
            return await _context.Books
                .Include(b => b.Reviews)
                .Include(b => b.Chapters)
                .Where(b => b.Reviews.Any())
                .OrderByDescending(b => b.Reviews.Average(r => r.Rating))
                .ThenByDescending(b => b.TotalViews)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetNewUpdatesAsync(int count = 15)
        {
            return await _context.Books
                .Include(b => b.Reviews)
                .Include(b => b.Chapters)
                .OrderByDescending(b => b.LastUpdated)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetTopRatedBooksAsync(int count = 10)
        {
            return await _context.Books
                .Include(b => b.Reviews)
                .Where(b => b.Reviews.Any())
                .OrderByDescending(b => b.Reviews.Average(r => r.Rating))
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetMostViewedBooksAsync(int count = 10)
        {
            return await _context.Books
                .OrderByDescending(b => b.TotalViews)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetMostFavoritedBooksAsync(int count = 10)
        {
            return await _context.Books
                .Select(b => new
                {
                    Book = b,
                    FavCount = _context.Favorites.Count(f => f.BookId == b.BookId)
                })
                .Where(x => x.FavCount > 0)
                .OrderByDescending(x => x.FavCount)
                .Take(count)
                .Select(x => x.Book)
                .ToListAsync();
        }

        public async Task IncrementViewCountAsync(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book != null)
            {
                book.TotalViews++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> UpdateCoverImageAsync(int bookId, string coverUrl)
        {
            try
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book == null) return false;

                book.CoverImageUrl = coverUrl;
                book.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> BookExistsAsync(int id)
        {
            return await _context.Books.AnyAsync(b => b.BookId == id);
        }

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}