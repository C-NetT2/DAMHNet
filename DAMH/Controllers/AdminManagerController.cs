using DAMH.Models;
using DAMH.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DAMH.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AdminManagerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IActivityLogService _activityLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminManagerController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IActivityLogService activityLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _activityLogService = activityLogService;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
        }

        private string GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        public async Task<IActionResult> Index()
        {
            var adminRole = await _roleManager.FindByNameAsync("Admin");
            if (adminRole == null) return View(new List<ApplicationUser>());

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");

            var allManageableUsers = admins.Concat(superAdmins).OrderBy(a => a.Email).ToList();
            return View(allManageableUsers);
        }

        [HttpGet]
        public IActionResult CreateAdmin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Email này đã tồn tại trong hệ thống.");
                return View(model);
            }

            var adminUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                IsMember = false,
                SubscriptionExpiryDate = null,
                RegistrationDate = DateTime.Now
            };

            var result = await _userManager.CreateAsync(adminUser, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");

                // FIXED: Use "SuperAdmin" section to distinguish from regular Admin actions
                await _activityLogService.LogActivityAsync(
                    userId: GetUserId(),
                    action: "Create",
                    section: "SuperAdmin",  // Changed from "Admin" to "SuperAdmin"
                    entityId: adminUser.Id,
                    entityName: adminUser.Email,
                    newValues: new
                    {
                        Email = adminUser.Email,
                        Role = "Admin",
                        CreatedDate = adminUser.RegistrationDate
                    },
                    notes: $"SuperAdmin tạo tài khoản Admin mới: {adminUser.Email}",
                    ipAddress: GetClientIpAddress()
                );

                TempData["SuccessMessage"] = $"Đã tạo tài khoản Admin: {model.Email}";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditAdmin(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);
            if (admin == null) return NotFound();

            var isAdmin = await _userManager.IsInRoleAsync(admin, "Admin");
            var isSuperAdmin = await _userManager.IsInRoleAsync(admin, "SuperAdmin");

            if (!isAdmin && !isSuperAdmin)
            {
                TempData["ErrorMessage"] = "Người dùng này không phải Admin hoặc SuperAdmin.";
                return RedirectToAction(nameof(Index));
            }

            var model = new EditAdminViewModel
            {
                Id = admin.Id,
                Email = admin.Email ?? "",
                FullName = admin.FullName
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdmin(EditAdminViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var admin = await _userManager.FindByIdAsync(model.Id);
            if (admin == null) return NotFound();

            var oldFullName = admin.FullName;

            admin.FullName = model.FullName;
            bool passwordChanged = !string.IsNullOrEmpty(model.NewPassword);

            if (passwordChanged)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
                var result = await _userManager.ResetPasswordAsync(admin, token, model.NewPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            var updateResult = await _userManager.UpdateAsync(admin);
            if (updateResult.Succeeded)
            {
                // FIXED: Use "SuperAdmin" section
                await _activityLogService.LogActivityAsync(
                    userId: GetUserId(),
                    action: "Update",
                    section: "SuperAdmin",  // Changed from "Admin" to "SuperAdmin"
                    entityId: admin.Id,
                    entityName: admin.Email,
                    oldValues: new
                    {
                        FullName = oldFullName,
                        PasswordChanged = false
                    },
                    newValues: new
                    {
                        FullName = model.FullName,
                        PasswordChanged = passwordChanged
                    },
                    notes: $"SuperAdmin cập nhật thông tin Admin: {admin.Email}" +
                           (passwordChanged ? " (Đã đổi mật khẩu)" : ""),
                    ipAddress: GetClientIpAddress()
                );

                TempData["SuccessMessage"] = "Cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdmin(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);
            if (admin == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Admin này.";
                return RedirectToAction(nameof(Index));
            }

            var isSuperAdmin = await _userManager.IsInRoleAsync(admin, "SuperAdmin");
            if (isSuperAdmin)
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản SuperAdmin.";
                return RedirectToAction(nameof(Index));
            }

            var adminEmail = admin.Email;
            var adminFullName = admin.FullName;
            var roles = await _userManager.GetRolesAsync(admin);

            var result = await _userManager.DeleteAsync(admin);
            if (result.Succeeded)
            {
                // FIXED: Use "SuperAdmin" section
                await _activityLogService.LogActivityAsync(
                    userId: GetUserId(),
                    action: "Delete",
                    section: "SuperAdmin",  // Changed from "Admin" to "SuperAdmin"
                    entityId: id,
                    entityName: adminEmail,
                    oldValues: new
                    {
                        Email = adminEmail,
                        FullName = adminFullName,
                        Roles = string.Join(", ", roles),
                        DeletedDate = DateTime.Now
                    },
                    notes: $"SuperAdmin xóa tài khoản Admin: {adminEmail}",
                    ipAddress: GetClientIpAddress()
                );

                TempData["SuccessMessage"] = $"Đã xóa Admin: {adminEmail}";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa Admin.";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class CreateAdminViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Role { get; set; } = "Admin";
    }

    public class EditAdminViewModel
    {
        public string Id { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string? ConfirmNewPassword { get; set; }
    }
}