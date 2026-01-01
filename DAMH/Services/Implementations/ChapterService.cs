using DAMH.Data;
using DAMH.Models;
using DAMH.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DAMH.Services.Implementations
{
    public class ChapterService : IChapterService
    {
        private readonly LibraryContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChapterService(LibraryContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Chapter?> GetChapterByIdAsync(int id)
        {
            return await _context.Chapters.FindAsync(id);
        }

        public async Task<Chapter?> GetChapterWithBookAsync(int id)
        {
            return await _context.Chapters
                .Include(c => c.Book)
                .ThenInclude(b => b.Chapters)
                .FirstOrDefaultAsync(c => c.ChapterId == id);
        }

        public async Task<IEnumerable<Chapter>> GetChaptersByBookIdAsync(int bookId)
        {
            return await _context.Chapters
                .Where(c => c.BookId == bookId)
                .OrderBy(c => c.ChapterOrder)
                .ToListAsync();
        }

        public async Task<bool> CreateChapterAsync(Chapter chapter)
        {
            try
            {
                _context.Chapters.Add(chapter);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateChapterAsync(Chapter chapter)
        {
            try
            {
                _context.Chapters.Update(chapter);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteChapterAsync(int id)
        {
            try
            {
                var chapter = await _context.Chapters.FindAsync(id);
                if (chapter == null) return false;

                _context.Chapters.Remove(chapter);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Chapter?> GetPreviousChapterAsync(int bookId, int currentChapterOrder)
        {
            return await _context.Chapters
                .Where(c => c.BookId == bookId && c.ChapterOrder < currentChapterOrder)
                .OrderByDescending(c => c.ChapterOrder)
                .FirstOrDefaultAsync();
        }

        public async Task<Chapter?> GetNextChapterAsync(int bookId, int currentChapterOrder)
        {
            return await _context.Chapters
                .Where(c => c.BookId == bookId && c.ChapterOrder > currentChapterOrder)
                .OrderBy(c => c.ChapterOrder)
                .FirstOrDefaultAsync();
        }

        public async Task<(int? PreviousId, int? NextId)> GetNavigationChapterIdsAsync(int chapterId)
        {
            var chapter = await GetChapterWithBookAsync(chapterId);
            if (chapter == null) return (null, null);

            var allChapters = chapter.Book.Chapters.OrderBy(c => c.ChapterOrder).ToList();
            var currentIndex = allChapters.FindIndex(c => c.ChapterId == chapterId);

            int? previousId = currentIndex > 0 ? allChapters[currentIndex - 1].ChapterId : null;
            int? nextId = currentIndex < allChapters.Count - 1 ? allChapters[currentIndex + 1].ChapterId : null;

            return (previousId, nextId);
        }

        public async Task<(bool Success, string Url, string HtmlTag)> UploadChapterMediaAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return (false, "", "");

                var extension = Path.GetExtension(file.FileName).ToLower();
                var allowedImageTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var allowedVideoTypes = new[] { ".mp4", ".webm", ".ogg" };

                bool isImage = allowedImageTypes.Contains(extension);
                bool isVideo = allowedVideoTypes.Contains(extension);

                if (!isImage && !isVideo)
                    return (false, "", "");

                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chapters");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var url = "/uploads/chapters/" + fileName;
                string htmlTag = isImage
                    ? $"<img src='{url}' class='img-fluid my-3 rounded shadow' alt='Minh họa' />"
                    : $"<video controls class='w-100 my-3 rounded shadow'><source src='{url}' type='video/mp4'></video>";

                return (true, url, htmlTag);
            }
            catch
            {
                return (false, "", "");
            }
        }

        public async Task<bool> ChapterExistsAsync(int id)
        {
            return await _context.Chapters.AnyAsync(c => c.ChapterId == id);
        }

        public async Task<bool> CanUserAccessChapterAsync(string userId, int chapterId)
        {
            var chapter = await GetChapterWithBookAsync(chapterId);
            if (chapter == null) return false;

            if (chapter.Book.AccessLevel == AccessLevel.Free || chapter.IsFree == true)
                return true;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            if (user.IsMember == true && (user.SubscriptionExpiryDate == null || user.SubscriptionExpiryDate > DateTime.Now))
                return true;

            return false;
        }
    }
}