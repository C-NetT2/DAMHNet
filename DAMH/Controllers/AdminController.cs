using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAMH.Data;
using DAMH.Models;

namespace DAMH.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới vào được
    public class AdminController : Controller
    {
        private readonly LibraryContext _context;

        public AdminController(LibraryContext context)
        {
            _context = context;
        }

        // 1. Danh sách sách
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books.ToListAsync();
            return View(books);
        }

        // 2. Tạo sách mới (Giao diện)
        [HttpGet]
        public IActionResult CreateBook()
        {
            return View();
        }

        // 3. Tạo sách mới (Xử lý)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBook(Book book)
        {
            if (ModelState.IsValid)
            {
                await _context.Books.AddAsync(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // 4. Thêm chương (Giao diện)
        [HttpGet]
        public async Task<IActionResult> AddChapter(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return NotFound();

            ViewBag.BookId = bookId;
            ViewBag.BookTitle = book.Title;
            return View();
        }

        // 5. Thêm chương (Xử lý)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChapter(Chapter chapter)
        {
            if (ModelState.IsValid)
            {
                await _context.Chapters.AddAsync(chapter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ViewBookChapters), new { bookId = chapter.BookId });
            }
            ViewBag.BookId = chapter.BookId;
            return View(chapter);
        }

        // 6. Xem các chương của sách
        [HttpGet]
        public async Task<IActionResult> ViewBookChapters(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.Chapters.OrderBy(c => c.ChapterOrder))
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (book == null) return NotFound();

            return View(book);
        }

        // 7. Sửa sách (Edit)
        [HttpGet]
        public async Task<IActionResult> EditBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook(int id, Book book)
        {
            if (id != book.BookId) return BadRequest();

            if (ModelState.IsValid)
            {
                _context.Update(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // 8. Xóa sách
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}