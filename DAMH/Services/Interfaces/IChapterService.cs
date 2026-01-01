using DAMH.Models;
using DAMH.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace DAMH.Services.Interfaces
{
    public interface IChapterService
    {
        Task<Chapter?> GetChapterByIdAsync(int id);
        Task<Chapter?> GetChapterWithBookAsync(int id);
        Task<IEnumerable<Chapter>> GetChaptersByBookIdAsync(int bookId);
        Task<bool> CreateChapterAsync(Chapter chapter);
        Task<bool> UpdateChapterAsync(Chapter chapter);
        Task<bool> DeleteChapterAsync(int id);
        Task<Chapter?> GetPreviousChapterAsync(int bookId, int currentChapterOrder);
        Task<Chapter?> GetNextChapterAsync(int bookId, int currentChapterOrder);
        Task<(int? PreviousId, int? NextId)> GetNavigationChapterIdsAsync(int chapterId);
        Task<(bool Success, string Url, string HtmlTag)> UploadChapterMediaAsync(IFormFile file);
        Task<bool> ChapterExistsAsync(int id);
        Task<bool> CanUserAccessChapterAsync(string userId, int chapterId);
    }
}
