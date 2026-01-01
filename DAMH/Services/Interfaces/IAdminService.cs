using DAMH.Models;

namespace DAMH.Services.Interfaces
{
    public interface IAdminService
    {
        Task<(bool Success, string Message)> CreateAdminAsync(string email, string password);
        Task<(bool Success, string Message)> UpdateAdminAsync(string userId, string? fullName, string? newPassword);
        Task<(bool Success, string Message)> DeleteAdminAsync(string userId);
        Task<IEnumerable<ApplicationUser>> GetAllAdminsAsync();
        Task<bool> IsAdminOrSuperAdminAsync(string userId);
    }
}
