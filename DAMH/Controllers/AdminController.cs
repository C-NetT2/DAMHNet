using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAMH.Data;
using DAMH.Models;

namespace DAMH.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới vào được các trang này
    public class AdminController : Controller
    {
        private readonly LibraryContext _context;

        public AdminController(LibraryContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. QUẢN LÝ SÁCH (INDEX)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books.ToListAsync();
            return View(books);
        }

        // ==========================================
        // 2. TẠO SÁCH MỚI (CREATE)
        // ==========================================
        [HttpGet]
        public IActionResult CreateBook()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBook(Book book)
        {
            if (ModelState.IsValid)
            {
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // ==========================================
        // 3. THÊM CHƯƠNG MỚI (ADD CHAPTER) - ĐÃ SỬA LỖI
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> AddChapter(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return NotFound("Không tìm thấy sách.");

            ViewBag.BookTitle = book.Title;

            // Tạo model mới và gán BookId ngay lập tức
            var newChapter = new Chapter
            {
                BookId = bookId,
                ChapterOrder = 1,
                IsFree = false
            };

            return View(newChapter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChapter(Chapter chapter)
        {
            // --- DÒNG QUAN TRỌNG NHẤT ĐỂ SỬA LỖI ---
            // Bỏ qua kiểm tra đối tượng Book (vì form chỉ gửi BookId)
            ModelState.Remove("Book");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(chapter);
                    await _context.SaveChangesAsync();
                    // Lưu thành công -> Quay về danh sách chương
                    return RedirectToAction(nameof(ViewBookChapters), new { bookId = chapter.BookId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi lưu dữ liệu: " + ex.Message);
                }
            }

            // Nếu lỗi -> Lấy lại tên sách để hiển thị lại form
            var book = await _context.Books.FindAsync(chapter.BookId);
            ViewBag.BookTitle = book?.Title ?? "Không tìm thấy sách";

            return View(chapter);
        }

        // ==========================================
        // 4. XEM DANH SÁCH CHƯƠNG
        // ==========================================
        public async Task<IActionResult> ViewBookChapters(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.Chapters)
                .FirstOrDefaultAsync(m => m.BookId == bookId);

            if (book == null) return NotFound();

            return View(book);
        }

        // ==========================================
        // 5. SỬA SÁCH (EDIT)
        // ==========================================
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
            if (id != book.BookId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Books.Any(e => e.BookId == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // ==========================================
        // 6. XÓA SÁCH (DELETE)
        // ==========================================
        [HttpPost, ActionName("DeleteBook")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 7. XÓA CHƯƠNG (DELETE CHAPTER)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChapter(int id)
        {
            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter != null)
            {
                int bookId = chapter.BookId; // Lưu lại ID để quay về đúng chỗ
                _context.Chapters.Remove(chapter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ViewBookChapters), new { bookId = bookId });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}