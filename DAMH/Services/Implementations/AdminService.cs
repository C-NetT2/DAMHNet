using DAMH.Models;
using DAMH.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace DAMH.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<(bool Success, string Message)> CreateAdminAsync(string email, string password)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null) return (false, "Email này đã tồn tại trong hệ thống.");

                var adminUser = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, IsMember = false, SubscriptionExpiryDate = null, RegistrationDate = DateTime.Now };
                var result = await _userManager.CreateAsync(adminUser, password);
                if (!result.Succeeded) return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

                await _userManager.AddToRoleAsync(adminUser, "Admin");
                return (true, $"Đã tạo tài khoản Admin: {email}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateAdminAsync(string userId, string? fullName, string? newPassword)
        {
            try
            {
                var admin = await _userManager.FindByIdAsync(userId);
                if (admin == null) return (false, "Không tìm thấy Admin.");

                admin.FullName = fullName;

                if (!string.IsNullOrEmpty(newPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
                    var result = await _userManager.ResetPasswordAsync(admin, token, newPassword);
                    if (!result.Succeeded) return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                var updateResult = await _userManager.UpdateAsync(admin);
                if (!updateResult.Succeeded) return (false, string.Join(", ", updateResult.Errors.Select(e => e.Description)));

                return (true, "Cập nhật thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAdminAsync(string userId)
        {
            try
            {
                var admin = await _userManager.FindByIdAsync(userId);
                if (admin == null) return (false, "Không tìm thấy Admin này.");
                var isSuperAdmin = await _userManager.IsInRoleAsync(admin, "SuperAdmin");
                if (isSuperAdmin) return (false, "Không thể xóa tài khoản SuperAdmin.");
                var result = await _userManager.DeleteAsync(admin);
                if (!result.Succeeded) return (false, "Có lỗi xảy ra khi xóa Admin.");
                return (true, $"Đã xóa Admin: {admin.Email}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<IEnumerable<ApplicationUser>> GetAllAdminsAsync()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            return admins.Concat(superAdmins).OrderBy(a => a.Email).ToList();
        }

        public async Task<bool> IsAdminOrSuperAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
            return isAdmin || isSuperAdmin;
        }
    }
}
