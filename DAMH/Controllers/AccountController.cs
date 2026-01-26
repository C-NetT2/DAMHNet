using DAMH.Models;
using DAMH.Models.ViewModels;
using DAMH.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DAMH.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserService _userService;
        private readonly IActivityLogService _activityLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserService userService,
            IActivityLogService activityLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userService = userService;
            _activityLogService = activityLogService;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var model = new LoginViewModel { ReturnUrl = returnUrl };
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                await _activityLogService.LogActivityAsync(
                    userId: user.Id,
                    action: "Login",
                    section: "Authentication",
                    entityId: user.Id,
                    entityName: user.Email,
                    notes: $"Đăng nhập thành công: {user.Email}",
                    ipAddress: GetClientIpAddress()
                );

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                IsMember = model.IsMember,
                SubscriptionExpiryDate = model.IsMember ? DateTime.Now.AddMonths(1) : null,
                RegistrationDate = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (model.IsMember)
                    await _userManager.AddToRoleAsync(user, "Member");
                else
                    await _userManager.AddToRoleAsync(user, "User");

                await _signInManager.SignInAsync(user, isPersistent: false);

                await _activityLogService.LogActivityAsync(
                    userId: user.Id,
                    action: "Register",
                    section: "User",
                    entityId: user.Id,
                    entityName: user.Email,
                    newValues: new
                    {
                        Email = user.Email,
                        IsMember = user.IsMember,
                        RegistrationDate = user.RegistrationDate
                    },
                    notes: $"Đăng ký tài khoản mới: {user.Email}" + (model.IsMember ? " (VIP trial)" : ""),
                    ipAddress: GetClientIpAddress()
                );

                TempData["SuccessMessage"] = "Đăng ký thành công!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = User.Identity?.Name;

            await _signInManager.SignOutAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogActivityAsync(
                    userId: userId,
                    action: "Logout",
                    section: "Authentication",
                    entityId: userId,
                    entityName: userEmail,
                    notes: $"Đăng xuất: {userEmail}",
                    ipAddress: GetClientIpAddress()
                );
            }

            TempData["SuccessMessage"] = "Đã đăng xuất.";
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login");

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                Email = user.Email ?? "",
                IsMember = user.IsMember,
                SubscriptionExpiryDate = user.SubscriptionExpiryDate,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                RegistrationDate = user.RegistrationDate
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login");

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound();

            var oldFullName = user.FullName;
            var oldPhoneNumber = user.PhoneNumber;
            var oldAddress = user.Address;

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;


            var result = await _userService.UpdateUserAsync(user);

            if (result)
            {
                await _activityLogService.LogActivityAsync(
                    userId: userId,
                    action: "Update",
                    section: "Profile",
                    entityId: userId,
                    entityName: user.Email,
                    oldValues: new
                    {
                        FullName = oldFullName,
                        PhoneNumber = oldPhoneNumber,
                        Address = oldAddress
                    },
                    newValues: new
                    {
                        FullName = model.FullName,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address
                    },
                    notes: $"Cập nhật thông tin cá nhân: {user.Email}",
                    ipAddress: GetClientIpAddress()
                );

                TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra");

            model.Email = user.Email ?? "";
            model.IsMember = user.IsMember;
            model.SubscriptionExpiryDate = user.SubscriptionExpiryDate;
            model.RegistrationDate = user.RegistrationDate;

            return View("Profile", model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("Profile");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                await _activityLogService.LogActivityAsync(
                    userId: userId!,
                    action: "Update",
                    section: "Security",
                    entityId: userId,
                    entityName: user.Email,
                    notes: $"Đổi mật khẩu thành công: {user.Email}",
                    ipAddress: GetClientIpAddress()
                );

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                TempData["ErrorMessage"] = error.Description;
            }

            return RedirectToAction("Profile");
        }
    }
}