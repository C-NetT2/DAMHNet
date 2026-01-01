using DAMH.Models;
using DAMH.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace DAMH.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<ApplicationUser?> GetUserWithDetailsAsync(string userId);
        Task<(IEnumerable<ApplicationUser> Users, int TotalCount)> GetPagedUsersAsync(int page, int pageSize, string searchTerm = "");
        Task<bool> UpdateUserAsync(ApplicationUser user);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> ExtendVipAsync(string userId, int months);
        Task<bool> RemoveVipAsync(string userId);
        Task<bool> IsUserVipActiveAsync(string userId);
        Task<int> GetActiveVipCountAsync();
        Task<int> GetTotalUsersAsync();
        Task<int> GetNewUsersThisMonthAsync();
        Task<int> GetNewUsersLastMonthAsync();
        Task<IEnumerable<ApplicationUser>> GetAdminUsersAsync();
        Task<bool> UserExistsAsync(string userId);
        Task<bool> IsAdminAsync(string userId);
        Task<bool> IsSuperAdminAsync(string userId);
    }
}
