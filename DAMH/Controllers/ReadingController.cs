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
                bool hasAccess = false;

                // 1. Nếu sách Free hoặc Chương Free -> Cho đọc
                if (book.AccessLevel == AccessLevel.Free || chapter.IsFree == true)
                {
                    hasAccess = true;
                }
                // 2. Nếu User đã đăng nhập + Là VIP + Còn hạn VIP -> Cho đọc
                else if (user != null && user.IsMember == true &&
                        (user.SubscriptionExpiryDate == null || user.SubscriptionExpiryDate > DateTime.Now))
                {
                    hasAccess = true;
                }

                if (!hasAccess)
                {
                    return View("AccessDenied", "Nội dung này chỉ dành cho thành viên VIP.");
                }

                // === LƯU LỊCH SỬ ĐỌC (NẾU ĐÃ ĐĂNG NHẬP) ===
                if (user != null)
                {
                    // Kiểm tra xem đã đọc sách này chưa
                    var existingHistory = await _context.ReadingHistories
                        .FirstOrDefaultAsync(rh => rh.UserId == user.Id && rh.BookId == book.BookId);

                    if (existingHistory != null)
                    {
                        // CẬP NHẬT: Lưu chương mới nhất vừa đọc
                        existingHistory.ChapterId = chapterId;
                        existingHistory.AccessTime = DateTime.Now;
                        _context.Update(existingHistory);
                    }
                    else
                    {
                        // TẠO MỚI: Lần đầu đọc sách này
                        var newHistory = new ReadingHistory
                        {
                            UserId = user.Id,
                            BookId = book.BookId,
                            ChapterId = chapterId,
                            AccessTime = DateTime.Now
                        };
                        _context.ReadingHistories.Add(newHistory);
                    }

                    await _context.SaveChangesAsync();
                }

                return View("Read", chapter);
            }
            catch (Exception ex)
            {
                return View("AccessDenied", ex.Message);
            }
        }
    }
}