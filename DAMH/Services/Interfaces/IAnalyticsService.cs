using DAMH.Models.ViewModels;

namespace DAMH.Services.Interfaces
{
    public interface IAnalyticsService
    {
        Task<AdvancedAnalyticsViewModel> GetAdvancedAnalyticsAsync();
        Task<Dictionary<string, int>> GetPackageSalesAsync();
        Task<List<GenreStatistic>> GetFavoriteGenreStatsAsync();
        Task<List<BookStatistic>> GetMostFavoritedBooksAsync(int count = 10);
        Task<List<BookStatistic>> GetMostReadBooksAsync(int count = 10);
        Task<int> GetNewUsersThisMonthAsync();
        Task<int> GetNewUsersLastMonthAsync();
        Task<double> GetUserGrowthPercentageAsync();
        Task<int> GetNewVipThisMonthAsync();
        Task<int> GetNewVipLastMonthAsync();
        Task<double> GetVipGrowthPercentageAsync();
        Task<decimal> GetRevenueThisMonthAsync();
        Task<decimal> GetRevenueLastMonthAsync();
        Task<double> GetRevenueGrowthPercentageAsync();
    }
}
