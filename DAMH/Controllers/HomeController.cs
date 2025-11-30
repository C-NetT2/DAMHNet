using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAMH.Data;
using DAMH.Models;
using DAMH.Models.ViewModels;

namespace DAMH.Controllers
{
    public class HomeController : Controller
    {
        private readonly LibraryContext _context;

        public HomeController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
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
                .Take(12)
                .ToListAsync();

            viewModel.TopRatedBooks = await _context.Books
                .Include(b => b.Reviews)
                .Where(b => b.Reviews.Any())
                .OrderByDescending(b => b.Reviews.Average(r => r.Rating))
                .ThenByDescending(b => b.Reviews.Count)
                .Take(10)
                .ToListAsync();

            return View(viewModel);
        }

        public async Task<IActionResult> Search(SearchViewModel model)
        {
            var query = _context.Books
                .Include(b => b.Reviews)
                .Include(b => b.Chapters)
                .AsQueryable();

            if (model.BookType.HasValue) query = query.Where(b => b.BookType == model.BookType.Value);
            if (model.Genre.HasValue) query = query.Where(b => b.Genre == model.Genre.Value);
            if (model.AgeRating.HasValue) query = query.Where(b => b.AgeRating == model.AgeRating.Value);

            var allBooks = await query.OrderByDescending(b => b.LastUpdated).ToListAsync();
            var resultBooks = new List<Book>();

            if (!string.IsNullOrWhiteSpace(model.Keyword))
            {
                string searchStr = model.Keyword.ToLower().Trim();
                var keywords = searchStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var book in allBooks)
                {
                    string bookInfo = $"{book.Title} {book.Author} {book.Description}".ToLower();

                    bool isMatch = keywords.Any(k => bookInfo.Contains(k));

                    if (isMatch)
                    {
                        resultBooks.Add(book);
                    }
                }
            }
            else
            {
                resultBooks = allBooks;
            }

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
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userReview = book.Reviews.FirstOrDefault(r => r.UserId == userId);

                if (userReview != null)
                {
                    viewModel.HasUserReviewed = true;
                    viewModel.UserRating = userReview.Rating;
                }
            }

            return View(viewModel);
        }
    }
}