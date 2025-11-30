using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DAMH.Models;
using DAMH.Models.ViewModels;

namespace DAMH.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // === 1. ĐĂNG KÝ (REGISTER) ===
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Tạo đối tượng User từ dữ liệu nhập vào
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    IsMember = model.IsMember, // Lưu trạng thái VIP
                    // Nếu chọn VIP thì tặng 1 tháng, không thì để null
                    SubscriptionExpiryDate = model.IsMember ? DateTime.Now.AddMonths(1) : null
                };

                // Lệnh tạo user của Identity
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Tự động đăng nhập luôn sau khi tạo xong
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                // Nếu lỗi (vd: trùng email, mật khẩu yếu) thì hiện lỗi ra
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        // === 2. ĐĂNG NHẬP (LOGIN) ===
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Giữ lại đường dẫn cũ để login xong quay lại đúng chỗ đó
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            returnUrl ??= Url.Content("~/"); // Nếu không có link cũ thì về trang chủ

            if (ModelState.IsValid)
            {
                // Kiểm tra đăng nhập
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Đăng nhập thất bại. Sai email hoặc mật khẩu.");
                }
            }
            return View(model);
        }

        // === 3. ĐĂNG XUẤT (LOGOUT) ===
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // === 4. TRANG BỊ TỪ CHỐI (ACCESS DENIED) ===
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}