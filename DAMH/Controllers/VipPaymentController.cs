using DAMH.Data;
using DAMH.Helpers;
using DAMH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DAMH.Controllers
{
    [Authorize]
    public class VipPaymentController : Controller
    {
        private readonly LibraryContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VipPaymentController(LibraryContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            ViewBag.CurrentUser = user;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(VipPackageType packageType, string fullName, string phoneNumber)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            try
            {
                var newExpiryDate = VipPackageHelper.CalculateExpiryDate(user.SubscriptionExpiryDate, packageType);
                var amount = VipPackageHelper.GetPrice(packageType);

                var transaction = new PaymentTransaction
                {
                    UserId = userId,
                    PackageType = packageType,
                    Amount = amount,
                    TransactionDate = DateTime.Now,
                    Status = "Completed",
                    Notes = $"Mua gói VIP: {packageType.GetName()}"
                };

                _context.PaymentTransactions.Add(transaction);

                user.IsMember = true;
                user.SubscriptionExpiryDate = newExpiryDate;

                if (!string.IsNullOrEmpty(fullName))
                    user.FullName = fullName;
                if (!string.IsNullOrEmpty(phoneNumber))
                    user.PhoneNumber = phoneNumber;

                await _userManager.UpdateAsync(user);
                
                var isMember = await _userManager.IsInRoleAsync(user, "Member");
                if (!isMember)
                {
                    await _userManager.AddToRoleAsync(user, "Member");
                }
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Thanh toán thành công! Tài khoản VIP của bạn đã được kích hoạt đến {newExpiryDate:dd/MM/yyyy}.";
                return RedirectToAction("PaymentSuccess", new { transactionId = transaction.TransactionId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình thanh toán. Vui lòng thử lại!";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int transactionId)
        {
            var transaction = await _context.PaymentTransactions.FindAsync(transactionId);
            if (transaction == null) return NotFound();

            var user = await _userManager.FindByIdAsync(transaction.UserId);
            ViewBag.Transaction = transaction;
            ViewBag.User = user;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PaymentHistory()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var transactions = _context.PaymentTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();

            return View(transactions);
        }
    }
}