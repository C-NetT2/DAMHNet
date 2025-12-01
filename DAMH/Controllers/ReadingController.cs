using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DAMH.Data;
using DAMH.Models;

namespace DAMH.Controllers
{
    public class ReadingController : Controller
    {
        private readonly LibraryContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReadingController(LibraryContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> ViewChapter(int chapterId, bool isAdminView = false)
        {
            try
            {
                var chapter = await _context.Chapters
                    .Include(c => c.Book).ThenInclude(b => b.Chapters)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId);

                if (chapter == null) return NotFound("Không tìm thấy chương.");

                var book = chapter.Book;
                var user = await _userManager.GetUserAsync(User);

                // Kiểm tra quyền (VIP/Free)
                bool hasAccess = false;
                if (book.AccessLevel == AccessLevel.Free || chapter.IsFree == true) hasAccess = true;
                else if (user != null && user.IsMember == true && (user.SubscriptionExpiryDate == null || user.SubscriptionExpiryDate > DateTime.Now)) hasAccess = true;

                // Admin luôn được xem
                if (User.IsInRole("Admin")) hasAccess = true;

                if (!hasAccess) return View("AccessDenied", "Nội dung VIP.");

                // Lưu lịch sử (chỉ user thường)
                if (user != null && !User.IsInRole("Admin"))
                {
                    var existingHistory = await _context.ReadingHistories.FirstOrDefaultAsync(rh => rh.UserId == user.Id && rh.BookId == book.BookId);
                    if (existingHistory != null) { existingHistory.ChapterId = chapterId; existingHistory.AccessTime = DateTime.Now; _context.Update(existingHistory); }
                    else { _context.ReadingHistories.Add(new ReadingHistory { UserId = user.Id, BookId = book.BookId, ChapterId = chapterId, AccessTime = DateTime.Now }); }
                    await _context.SaveChangesAsync();
                }

                // === LOGIC TÌM CHƯƠNG TRƯỚC / SAU ===
                var allChapters = book.Chapters.OrderBy(c => c.ChapterOrder).ToList();
                var currentIndex = allChapters.FindIndex(c => c.ChapterId == chapterId);

                int? previousChapterId = null;
                int? nextChapterId = null;

                if (currentIndex > 0) previousChapterId = allChapters[currentIndex - 1].ChapterId;
                if (currentIndex < allChapters.Count - 1) nextChapterId = allChapters[currentIndex + 1].ChapterId;

                // Truyền dữ liệu sang View
                ViewBag.PreviousChapterId = previousChapterId;
                ViewBag.NextChapterId = nextChapterId;
                ViewBag.AllChapters = allChapters;
                ViewBag.IsAdminView = isAdminView; // QUAN TRỌNG: Để biết đường quay về

                return View("Read", chapter);
            }
            catch (Exception ex)
            {
                return View("AccessDenied", ex.Message);
            }
        }
    }
}