using DAMH.Models;
using DAMH.Models.ViewModels;

namespace DAMH.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<(bool Success, string Message, int TransactionId)> ProcessPaymentAsync(string userId, VipPackageType packageType, string? fullName = null, string? phoneNumber = null);
        Task<PaymentTransaction?> GetTransactionByIdAsync(int transactionId);
        Task<IEnumerable<PaymentTransaction>> GetUserTransactionsAsync(string userId);
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, int>> GetPackageSalesAsync();
        Task<IEnumerable<MonthlyRevenueData>> GetMonthlyRevenueAsync(int months = 6);
        Task<int> GetNewVipCountByPeriodAsync(DateTime startDate, DateTime endDate);
    }
}
