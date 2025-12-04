namespace DAMH.Models.ViewModels
{
    public class AdvancedAnalyticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalBooks { get; set; }
        public int TotalFavorites { get; set; }
        public int TotalReadings { get; set; }

        public int NewUsersThisMonth { get; set; }
        public int NewUsersLastMonth { get; set; }
        public double UserGrowthPercentage { get; set; }

        public int TotalVipUsers { get; set; }
        public int NewVipThisMonth { get; set; }
        public int NewVipLastMonth { get; set; }
        public double VipGrowthPercentage { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
        public double RevenueGrowthPercentage { get; set; }

        public Dictionary<string, int> PackageSales { get; set; } = new();

        public List<GenreStatistic> FavoriteGenreStats { get; set; } = new();
        public List<BookStatistic> MostFavoritedBooks { get; set; } = new();
        public List<BookStatistic> MostReadBooks { get; set; } = new();

        public List<MonthlyRevenueData> MonthlyRevenue { get; set; } = new();
    }

    public class MonthlyRevenueData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int VipCount { get; set; }
    }
}