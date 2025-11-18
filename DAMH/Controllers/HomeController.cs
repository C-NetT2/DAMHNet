using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAMH.Data;
using DAMH.Models;
using System.Diagnostics;

namespace DAMH.Controllers
{
    public class HomeController : Controller
    {
        private readonly LibraryContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(LibraryContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Hiển thị tất cả sách và chương
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books
                .Include(b => b.Chapters)
                .ToListAsync();

            return View(books);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}