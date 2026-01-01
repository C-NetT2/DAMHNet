using DAMH.Data;
using DAMH.Helpers;
using DAMH.Models.ViewModels;
using DAMH.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAMH.Services.Implementations
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly LibraryContext _context;

        public AnalyticsService(LibraryContext context)
        {
            _context = context;
        }

        public async Task<AdvancedAnalyticsViewModel> GetAdvancedAnalyticsAsync()
        {
            var viewModel = new AdvancedAnalyticsViewModel();

            viewModel.TotalUsers = await _context.Users.CountAsync();
            viewModel.TotalBooks = await _context.Books.CountAsync();
            viewModel.TotalFavorites = await _context.Favorites.CountAsync();
            viewModel.TotalReadings = await _context.ReadingHistories.CountAsync();

            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            viewModel.NewUsersThisMonth = await GetNewUsersThisMonthAsync();
            viewModel.NewUsersLastMonth = await GetNewUsersLastMonthAsync();
            viewModel.UserGrowthPercentage = await GetUserGrowthPercentageAsync();

            viewModel.TotalVipUsers = await _context.Users.Where(u => u.IsMember && u.SubscriptionExpiryDate > DateTime.Now).CountAsync();

            var vipTransactionsThisMonth = await _context.PaymentTransactions.Where(t => t.TransactionDate >= thisMonthStart && t.Status == "Completed").ToListAsync();
            var vipTransactionsLastMonth = await _context.PaymentTransactions.Where(t => t.TransactionDate >= lastMonthStart && t.TransactionDate < thisMonthStart && t.Status == "Completed").ToListAsync();

            viewModel.NewVipThisMonth = vipTransactionsThisMonth.Count;
            viewModel.NewVipLastMonth = vipTransactionsLastMonth.Count;
            viewModel.VipGrowthPercentage = await GetVipGrowthPercentageAsync();

            viewModel.TotalRevenue = await _context.PaymentTransactions.Where(t => t.Status == "Completed").SumAsync(t => t.Amount);
            viewModel.RevenueThisMonth = vipTransactionsThisMonth.Sum(t => t.Amount);
            viewModel.RevenueLastMonth = vipTransactionsLastMonth.Sum(t => t.Amount);
            viewModel.RevenueGrowthPercentage = await GetRevenueGrowthPercentageAsync();

            viewModel.PackageSales = await GetPackageSalesAsync();

            for (int i = 5; i >= 0; i--)
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
                    .FirstOrDefaultAsync() ?? new MonthlyRevenueData
                    {
                        Month = monthStart.ToString("MM/yyyy"),
                        Revenue = 0,
                        VipCount = 0
                    };

                viewModel.MonthlyRevenue.Add(monthlyData);
            }

            viewModel.FavoriteGenreStats = await GetFavoriteGenreStatsAsync();
            viewModel.MostFavoritedBooks = await GetMostFavoritedBooksAsync(10);
            viewModel.MostReadBooks = await GetMostReadBooksAsync(10);

            return viewModel;
        }

        public async Task<Dictionary<string, int>> GetPackageSalesAsync()
        {
            var packageSales = await _context.PaymentTransactions.Where(t => t.Status == "Completed").GroupBy(t => t.PackageType).Select(g => new { Package = g.Key, Count = g.Count() }).ToListAsync();
            var result = new Dictionary<string, int>();
            foreach (var sale in packageSales) result[sale.Package.GetName()] = sale.Count;
            return result;
        }

        public async Task<List<GenreStatistic>> GetFavoriteGenreStatsAsync()
        {
            var favGenres = await _context.Favorites.Include(f => f.Book).GroupBy(f => f.Book.Genre).Select(g => new { Genre = g.Key, Count = g.Count() }).ToListAsync();
            int totalFavs = favGenres.Sum(g => g.Count);
            if (totalFavs == 0) return new List<GenreStatistic>();
            return favGenres.Select(g => new GenreStatistic { Genre = g.Genre, Count = g.Count, Percentage = Math.Round((double)g.Count / totalFavs * 100, 1) }).OrderByDescending(s => s.Percentage).ToList();
        }

        public async Task<List<BookStatistic>> GetMostFavoritedBooksAsync(int count = 10)
        {
            return await _context.Books.Select(b => new BookStatistic { Book = b, FavoriteCount = _context.Favorites.Count(f => f.BookId == b.BookId) }).OrderByDescending(b => b.FavoriteCount).Take(count).ToListAsync();
        }

        public async Task<List<BookStatistic>> GetMostReadBooksAsync(int count = 10)
        {
            return await _context.Books.Select(b => new BookStatistic { Book = b, ReadCount = _context.ReadingHistories.Count(rh => rh.BookId == b.BookId) }).OrderByDescending(b => b.ReadCount).Take(count).ToListAsync();
        }

        public async Task<int> GetNewUsersThisMonthAsync()
        {
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return await _context.Users.Where(u => u.RegistrationDate >= thisMonthStart).CountAsync();
        }

        public async Task<int> GetNewUsersLastMonthAsync()
        {
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            return await _context.Users.Where(u => u.RegistrationDate >= lastMonthStart && u.RegistrationDate < thisMonthStart).CountAsync();
        }

        public async Task<double> GetUserGrowthPercentageAsync()
        {
            var thisMonth = await GetNewUsersThisMonthAsync();
            var lastMonth = await GetNewUsersLastMonthAsync();
            return lastMonth > 0 ? Math.Round(((double)(thisMonth - lastMonth) / lastMonth) * 100, 1) : thisMonth > 0 ? 100 : 0;
        }

        public async Task<int> GetNewVipThisMonthAsync()
        {
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return await _context.PaymentTransactions.Where(t => t.TransactionDate >= thisMonthStart && t.Status == "Completed").CountAsync();
        }

        public async Task<int> GetNewVipLastMonthAsync()
        {
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            return await _context.PaymentTransactions.Where(t => t.TransactionDate >= lastMonthStart && t.TransactionDate < thisMonthStart && t.Status == "Completed").CountAsync();
        }

        public async Task<double> GetVipGrowthPercentageAsync()
        {
            var thisMonth = await GetNewVipThisMonthAsync();
            var lastMonth = await GetNewVipLastMonthAsync();
            return lastMonth > 0 ? Math.Round(((double)(thisMonth - lastMonth) / lastMonth) * 100, 1) : thisMonth > 0 ? 100 : 0;
        }

        public async Task<decimal> GetRevenueThisMonthAsync()
        {
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return await _context.PaymentTransactions.Where(t => t.TransactionDate >= thisMonthStart && t.Status == "Completed").SumAsync(t => t.Amount);
        }

        public async Task<decimal> GetRevenueLastMonthAsync()
        {
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            return await _context.PaymentTransactions.Where(t => t.TransactionDate >= lastMonthStart && t.TransactionDate < thisMonthStart && t.Status == "Completed").SumAsync(t => t.Amount);
        }

        public async Task<double> GetRevenueGrowthPercentageAsync()
        {
            var thisMonth = await GetRevenueThisMonthAsync();
            var lastMonth = await GetRevenueLastMonthAsync();
            return lastMonth > 0 ? Math.Round(((double)(thisMonth - lastMonth) / (double)lastMonth) * 100, 1) : thisMonth > 0 ? 100 : 0;
        }
    }


}