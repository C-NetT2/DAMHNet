using DAMH.Models;
using DAMH.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace DAMH.Services.Interfaces
{
    public interface IMediaService
    {
        Task<(bool Success, string Message, string Url)> UploadCoverImageAsync(int bookId, IFormFile? file, string? url);
        Task<bool> DeleteCoverImageAsync(int bookId);
        Task<(bool Success, string Message)> AddBookMediaAsync(int bookId, IFormFile? file, string? url, MediaType mediaType);
        Task<bool> DeleteBookMediaAsync(int mediaId);
        Task<IEnumerable<BookMedia>> GetBookMediaAsync(int bookId);
        Task<(bool Success, string Url, string HtmlTag)> UploadChapterMediaAsync(IFormFile file);
    }
}
