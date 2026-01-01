using DAMH.Data;
using DAMH.Helpers;
using DAMH.Models;
using DAMH.Models.ViewModels;
using DAMH.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DAMH.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly LibraryContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentService(LibraryContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<(bool Success, string Message, int TransactionId)> ProcessPaymentAsync(string userId, VipPackageType packageType, string? fullName = null, string? phoneNumber = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return (false, "Không tìm thấy người dùng", 0);

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

                return (true, "Thanh toán thành công", transaction.TransactionId);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}", 0);
            }
        }

        public async Task<PaymentTransaction?> GetTransactionByIdAsync(int transactionId)
        {
            return await _context.PaymentTransactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<IEnumerable<PaymentTransaction>> GetUserTransactionsAsync(string userId)
        {
            return await _context.PaymentTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.PaymentTransactions
                .Where(t => t.Status == "Completed")
                .SumAsync(t => t.Amount);
        }

        public async Task<decimal> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.PaymentTransactions
                .Where(t => t.Status == "Completed" && t.TransactionDate >= startDate && t.TransactionDate < endDate)
                .SumAsync(t => t.Amount);
        }

        public async Task<Dictionary<string, int>> GetPackageSalesAsync()
        {
            var packageSales = await _context.PaymentTransactions
                .Where(t => t.Status == "Completed")
                .GroupBy(t => t.PackageType)
                .Select(g => new { Package = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<string, int>();
            foreach (var sale in packageSales)
            {
                result[sale.Package.GetName()] = sale.Count;
            }
            return result;
        }

        public async Task<IEnumerable<MonthlyRevenueData>> GetMonthlyRevenueAsync(int months = 6)
        {
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var result = new List<MonthlyRevenueData>();

            for (int i = months - 1; i >= 0; i--)
            {
                var monthStart = thisMonthStart.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var monthlyData = await _context.PaymentTransactions
                    .Where(t => t.TransactionDate >= monthStart && t.TransactionDate < monthEnd && t.Status == "Completed")
                    .GroupBy(t => 1)
                    .Select(g => new MonthlyRevenueData
                    {
                        Month = monthStart.ToString("MM/yyyy"),
                        Revenue = g.Sum(t => t.Amount),
                        VipCount = g.Count()
                    })
                    .FirstOrDefaultAsync();

                if (monthlyData == null)
                {
                    monthlyData = new MonthlyRevenueData
                    {
                        Month = monthStart.ToString("MM/yyyy"),
                        Revenue = 0,
                        VipCount = 0
                    };
                }

                result.Add(monthlyData);
            }

            return result;
        }

        public async Task<int> GetNewVipCountByPeriodAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.PaymentTransactions
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate < endDate && t.Status == "Completed")
                .CountAsync();
        }
    }
}