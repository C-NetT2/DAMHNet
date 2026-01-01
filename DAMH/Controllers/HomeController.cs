using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DAMH.Models.ViewModels;
using DAMH.Services.Interfaces;
using System.Security.Claims;

namespace DAMH.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBookService _bookService;
        private readonly IReadingHistoryService _readingHistoryService;

        public HomeController(
            IBookService bookService,
            IReadingHistoryService readingHistoryService)
        {
            _bookService = bookService;
            _readingHistoryService = readingHistoryService;
        }

        public async Task<IActionResult> Index(string rankBy = "rating")
        {
            var viewModel = new HomeViewModel
            {
                HotStories = (await _bookService.GetHotBooksAsync(10)).ToList(),
                NewUpdates = (await _bookService.GetNewUpdatesAsync(15)).ToList()
            };

            viewModel.TopRatedBooks = rankBy.ToLower() switch
            {
                "views" => (await _bookService.GetMostViewedBooksAsync(10)).ToList(),
                "favorites" => (await _bookService.GetMostFavoritedBooksAsync(10)).ToList(),
                _ => (await _bookService.GetTopRatedBooksAsync(10)).ToList()
            };

            ViewBag.CurrentRankBy = rankBy;
            return View(viewModel);
        }

        public async Task<IActionResult> Search(SearchViewModel model)
        {
            var results = await _bookService.SearchBooksAsync(
                model.Keyword ?? "",
                model.BookType,
                model.Genre,
                model.AgeRating);

            ViewBag.SearchModel = model;
            return View(results.ToList());
        }

        public async Task<IActionResult> Details(int id)
        {
            var book = await _bookService.GetBookWithDetailsAsync(id);
            if (book == null) return NotFound();

            await _bookService.IncrementViewCountAsync(id);

            var viewModel = new BookDetailViewModel
            {
                Book = book,
                HasUserReviewed = false,
                UserRating = null
            };

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    // TODO: Implement through ReviewService
                    var userReview = book.Reviews.FirstOrDefault(r => r.UserId == userId);
                    if (userReview != null)
                    {
                        viewModel.HasUserReviewed = true;
                        viewModel.UserRating = userReview.Rating;
                    }
                }
            }

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            var history = await _readingHistoryService.GetUniqueUserHistoryAsync(userId);
            return View(history);
        }

        public async Task<IActionResult> AllBooks(int page = 1)
        {
            const int pageSize = 30;
            if (page < 1) page = 1;

            var (books, totalCount) = await _bookService.GetPagedBooksAsync(page, pageSize);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (page > totalPages && totalPages > 0) page = totalPages;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalBooks = totalCount;

            return View(books.ToList());
        }
    }
}