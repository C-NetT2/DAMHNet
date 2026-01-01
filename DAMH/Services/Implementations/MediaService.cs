using DAMH.Data;
using DAMH.Models;
using DAMH.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAMH.Services.Implementations
{
    public class MediaService : IMediaService
    {
        private readonly LibraryContext _context;

        public MediaService(LibraryContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, string Url)> UploadCoverImageAsync(int bookId, IFormFile? file, string? url)
        {
            try
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book == null) return (false, "Không tìm thấy sách", "");

                string finalUrl = "";

                if (file != null && file.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var extension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension)) return (false, "Chỉ chấp nhận file ảnh", "");

                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "covers");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    finalUrl = $"/uploads/covers/{fileName}";
                }
                else if (!string.IsNullOrWhiteSpace(url))
                {
                    finalUrl = url;
                }
                else
                {
                    return (false, "Vui lòng upload file hoặc nhập URL", "");
                }

                book.CoverImageUrl = finalUrl;
                book.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();

                return (true, "Cập nhật ảnh bìa thành công!", finalUrl);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}", "");
            }
        }

        public async Task<bool> DeleteCoverImageAsync(int bookId)
        {
            try
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book == null) return false;
                book.CoverImageUrl = null;
                book.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string Message)> AddBookMediaAsync(int bookId, IFormFile? file, string? url, MediaType mediaType)
        {
            try
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book == null) return (false, "Không tìm thấy sách");

                string finalUrl;

                if (file != null && file.Length > 0)
                {
                    var allowedExtensions = mediaType == MediaType.Image ? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" } : new[] { ".mp4", ".webm", ".ogg" };
                    var extension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension)) return (false, $"Định dạng file không hợp lệ");

                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "media");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    finalUrl = $"/uploads/media/{fileName}";
                }
                else if (!string.IsNullOrWhiteSpace(url))
                {
                    finalUrl = url;
                }
                else
                {
                    return (false, "Vui lòng upload file hoặc nhập URL");
                }

                var bookMedia = new BookMedia { BookId = bookId, Url = finalUrl, MediaType = mediaType, UploadedDate = DateTime.Now };
                _context.BookMedias.Add(bookMedia);
                await _context.SaveChangesAsync();

                return (true, "Thêm media thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<bool> DeleteBookMediaAsync(int mediaId)
        {
            try
            {
                var media = await _context.BookMedias.FindAsync(mediaId);
                if (media == null) return false;
                _context.BookMedias.Remove(media);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<BookMedia>> GetBookMediaAsync(int bookId)
        {
            return await _context.BookMedias.Where(m => m.BookId == bookId).OrderByDescending(m => m.UploadedDate).ToListAsync();
        }

        public async Task<(bool Success, string Url, string HtmlTag)> UploadChapterMediaAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0) return (false, "", "");
                var extension = Path.GetExtension(file.FileName).ToLower();
                var allowedImageTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var allowedVideoTypes = new[] { ".mp4", ".webm", ".ogg" };
                bool isImage = allowedImageTypes.Contains(extension);
                bool isVideo = allowedVideoTypes.Contains(extension);
                if (!isImage && !isVideo) return (false, "", "");

                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chapters");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var url = "/uploads/chapters/" + fileName;
                string htmlTag = isImage ? $"<img src='{url}' class='img-fluid my-3 rounded shadow' alt='Minh họa' />" : $"<video controls class='w-100 my-3 rounded shadow'><source src='{url}' type='video/mp4'></video>";
                return (true, url, htmlTag);
            }
            catch
            {
                return (false, "", "");
            }
        }
    }

}
