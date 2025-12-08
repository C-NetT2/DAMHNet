using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DAMH.Data;
using DAMH.Models;
using DAMH.Models.ViewModels;
using System.Security.Claims;
using System.Text;
using System.Globalization;

namespace DAMH.Controllers
{
    public class HomeController : Controller
    {
        private readonly LibraryContext _context;

        public HomeController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string rankBy = "rating")
        {
            var viewModel = new HomeViewModel();

            viewModel.HotStories = await _context.Books
                .Include(b => b.Reviews)
                .Include(b => b.Chapters)
                .Where(b => b.Reviews.Any())
                .OrderByDescending(b => b.Reviews.Average(r => r.Rating))
                .ThenByDescending(b => b.TotalViews)
                .Take(10)
                .ToListAsync();

            viewModel.NewUpdates = await _context.Books
                .Include(b => b.Reviews)
                .Include(b => b.Chapters)
                .OrderByDescending(b => b.LastUpdated)
                .Take(15)
                .ToListAsync();

            List<Book> topRatedBooks;
            switch (rankBy.ToLower())
            {
                case "views":
                    topRatedBooks = await _context.Books
                        .OrderByDescending(b => b.TotalViews)
                        .Take(10)
                        .ToListAsync();
                    break;

                case "favorites":
                    topRatedBooks = await _context.Books
                        .Select(b => new
                        {
                            Book = b,
                            FavCount = _context.Favorites.Count(f => f.BookId == b.BookId)
                        })
                        .Where(x => x.FavCount > 0)
                        .OrderByDescending(x => x.FavCount)
                        .Take(10)
                        .Select(x => x.Book)
                        .ToListAsync();
                    break;

                default: 
                    topRatedBooks = await _context.Books
                        .Include(b => b.Reviews)
                        .Where(b => b.Reviews.Any())
                        .OrderByDescending(b => b.Reviews.Average(r => r.Rating))
                        .Take(10)
                        .ToListAsync();
                    break;
            }

            viewModel.TopRatedBooks = topRatedBooks;
            ViewBag.CurrentRankBy = rankBy;

            return View(viewModel);
        }

        public async Task<IActionResult> Search(SearchViewModel model)
        {
            var query = _context.Books.Include(b => b.Reviews).Include(b => b.Chapters).AsQueryable();

            if (model.BookType.HasValue) query = query.Where(b => b.BookType == model.BookType.Value);
            if (model.Genre.HasValue) query = query.Where(b => b.Genre == model.Genre.Value);
            if (model.AgeRating.HasValue) query = query.Where(b => b.AgeRating == model.AgeRating.Value);

            var allBooks = await query.ToListAsync();
            var resultBooks = new List<Book>();

            if (!string.IsNullOrWhiteSpace(model.Keyword))
            {
                string searchStr = RemoveDiacritics(model.Keyword.ToLower());
                var tokens = searchStr.Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);

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
            }
            else resultBooks = allBooks;

            ViewBag.SearchModel = model;
            return View(resultBooks);
        }

        public async Task<IActionResult> Details(int id)
        {
            var book = await _context.Books
                .Include(b => b.Reviews).ThenInclude(r => r.User)
                .Include(b => b.Chapters.OrderBy(c => c.ChapterOrder))
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null) return NotFound();

            book.TotalViews++;
            await _context.SaveChangesAsync();

            var viewModel = new BookDetailViewModel
            {
                Book = book,
                HasUserReviewed = false,
                UserRating = null
            };

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userReview = book.Reviews.FirstOrDefault(r => r.UserId == userId);

                if (userReview != null)
                {
                    viewModel.HasUserReviewed = true;
                    viewModel.UserRating = userReview.Rating;
                }
            }

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rawHistory = await _context.ReadingHistories
                .Where(rh => rh.UserId == userId)
                .Include(rh => rh.Book)
                .Include(rh => rh.Chapter)
                .OrderByDescending(rh => rh.AccessTime)
                .ToListAsync();

            var uniqueHistory = rawHistory
                .GroupBy(rh => rh.BookId)
                .Select(g => g.First())
                .ToList();

            return View(uniqueHistory);
        }

        public async Task<IActionResult> AllBooks(int page = 1)
        {
            const int pageSize = 30;

            if (page < 1) page = 1;

            var totalBooks = await _context.Books.CountAsync();
            var totalPages = (int)Math.Ceiling(totalBooks / (double)pageSize);

            if (page > totalPages && totalPages > 0) page = totalPages;

            var books = await _context.Books
                .Include(b => b.Reviews)
                .Include(b => b.Chapters)
                .OrderByDescending(b => b.LastUpdated)
                .ThenByDescending(b => b.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalBooks = totalBooks;

            return View(books);
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