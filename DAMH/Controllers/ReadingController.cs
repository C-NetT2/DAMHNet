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

        public async Task<IActionResult> ViewChapter(int chapterId)
        {
            try
            {
                // Lấy chương sách và thông tin sách cha
                var chapter = await _context.Chapters
                    .Include(c => c.Book)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId);

                if (chapter == null)
                {
                    return NotFound("Không tìm thấy chương này.");
                }

                var book = chapter.Book;
                var user = await _userManager.GetUserAsync(User);

                // === LOGIC KIỂM TRA QUYỀN (FREEMIUM) ===
                // 1. Nếu sách Free hoặc Chương Free -> Cho đọc
                if (book.AccessLevel == AccessLevel.Free || chapter.IsFree == true)
                {
                    return View("Read", chapter);
                }
                // 2. Nếu User đã đăng nhập + Là VIP + Còn hạn VIP -> Cho đọc
                else if (user != null && user.IsMember == true && (user.SubscriptionExpiryDate == null || user.SubscriptionExpiryDate > DateTime.Now))
                {
                    return View("Read", chapter);
                }
                // 3. Còn lại -> Chặn (Yêu cầu mua VIP)
                else
                {
                    return View("AccessDenied", "Nội dung này chỉ dành cho thành viên VIP.");
                }
            }
            catch (Exception ex)
            {
                return View("AccessDenied", ex.Message);
            }
        }
    }
}