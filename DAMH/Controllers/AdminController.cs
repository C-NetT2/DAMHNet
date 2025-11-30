using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DAMH.Data;
using DAMH.Models;

namespace DAMH.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly LibraryContext _context;

        public AdminController(LibraryContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. QUẢN LÝ SÁCH
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books.ToListAsync();
            return View(books);
        }

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
                book.CreatedDate = DateTime.Now;
                book.LastUpdated = DateTime.Now;
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

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
            if (id != book.BookId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    book.LastUpdated = DateTime.Now;
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
        // 2. QUẢN LÝ CHƯƠNG (CHAPTERS)
        // ==========================================

        // --- HÀM BẠN ĐANG THIẾU ---
        public async Task<IActionResult> ViewBookChapters(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.Chapters.OrderBy(c => c.ChapterOrder))
                .FirstOrDefaultAsync(m => m.BookId == bookId);

            if (book == null) return NotFound();

            return View(book);
        }
        // ---------------------------

        [HttpGet]
        public async Task<IActionResult> AddChapter(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return NotFound("Không tìm thấy sách.");

            ViewBag.BookTitle = book.Title;

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
            ModelState.Remove("Book"); // Sửa lỗi validation

            if (ModelState.IsValid)
            {
                _context.Add(chapter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ViewBookChapters), new { bookId = chapter.BookId });
            }

            var book = await _context.Books.FindAsync(chapter.BookId);
            ViewBag.BookTitle = book?.Title;
            return View(chapter);
        }

        [HttpGet]
        public async Task<IActionResult> EditChapter(int id)
        {
            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter == null) return NotFound();
            return View(chapter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChapter(int id, Chapter chapter)
        {
            if (id != chapter.ChapterId) return NotFound();

            ModelState.Remove("Book"); // Sửa lỗi validation

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chapter);
                    await _context.SaveChangesAsync();
                    
                    return RedirectToAction(nameof(ViewBookChapters), new { bookId = chapter.BookId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Chapters.Any(e => e.ChapterId == id)) return NotFound();
                    else throw;
                }
            }
            return View(chapter);
        }

        [HttpPost]
        public async Task<IActionResult> UploadChapterMedia(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn file." });

            var extension = Path.GetExtension(file.FileName).ToLower();
            var allowedImageTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var allowedVideoTypes = new[] { ".mp4", ".webm", ".ogg" };

            bool isImage = allowedImageTypes.Contains(extension);
            bool isVideo = allowedVideoTypes.Contains(extension);

            if (!isImage && !isVideo)
                return Json(new { success = false, message = "Chỉ hỗ trợ file ảnh hoặc video (mp4)." });

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chapters");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            var fileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var url = "/uploads/chapters/" + fileName;

            string htmlTag;
            if (isImage)
                htmlTag = $"<img src='{url}' class='img-fluid my-3 rounded shadow' alt='Minh họa' />";
            else
                htmlTag = $"<video controls class='w-100 my-3 rounded shadow'><source src='{url}' type='video/mp4'></video>";

            return Json(new { success = true, url = url, html = htmlTag });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChapter(int id)
        {
            var chapter = await _context.Chapters.FindAsync(id);
            if (chapter != null)
            {
                int bookId = chapter.BookId;
                _context.Chapters.Remove(chapter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ViewBookChapters), new { bookId = bookId });
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 3. QUẢN LÝ MEDIA (ẢNH/VIDEO)
        // ==========================================
        public async Task<IActionResult> ManageMedia(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.MediaFiles)
                .FirstOrDefaultAsync(b => b.BookId == bookId);

            if (book == null) return NotFound();
            return View(book);
        }

        [HttpGet]
        public IActionResult AddMedia(int bookId)
        {
            ViewBag.BookId = bookId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMedia(BookMedia media)
        {
            // Fix lỗi validation nếu cần
            ModelState.Remove("Book");

            if (ModelState.IsValid)
            {
                media.UploadedDate = DateTime.Now;
                _context.BookMedias.Add(media);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageMedia), new { bookId = media.BookId });
            }
            ViewBag.BookId = media.BookId;
            return View(media);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var media = await _context.BookMedias.FindAsync(id);
            if (media != null)
            {
                int bookId = media.BookId;
                _context.BookMedias.Remove(media);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageMedia), new { bookId = bookId });
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 4. QUẢN LÝ NGƯỜI DÙNG
        // ==========================================
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _context.Users
                .Include(u => u.ReadingHistories).ThenInclude(rh => rh.Book)
                .Include(u => u.ReadingHistories).ThenInclude(rh => rh.Chapter)
                .Include(u => u.Reviews).ThenInclude(r => r.Book)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();
            return View(user);
        }

        // ==========================================
        // 5. QUẢN LÝ ĐÁNH GIÁ (REVIEWS)
        // ==========================================
        public async Task<IActionResult> ManageReviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.Book)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
            return View(reviews);
        }

        [HttpGet]
        public async Task<IActionResult> EditReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();
            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(int id, Review review)
        {
            if (id != review.ReviewId) return NotFound();

            var existingReview = await _context.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.ReviewId == id);
            if (existingReview == null) return NotFound();

            // Giữ lại các thông tin không được sửa
            review.UserId = existingReview.UserId;
            review.BookId = existingReview.BookId;
            review.CreatedDate = existingReview.CreatedDate;
            review.UpdatedDate = DateTime.Now;

            // Fix lỗi validation
            ModelState.Remove("User");
            ModelState.Remove("Book");

            if (ModelState.IsValid)
            {
                _context.Update(review);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageReviews));
            }
            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageReviews));
        }
    }
}