namespace DAMH.Models.ViewModels
{
    public class AdminAnalyticsViewModel
    {
        // Tổng quan
        public int TotalUsers { get; set; }
        public int TotalBooks { get; set; }
        public int TotalFavorites { get; set; }
        public int TotalReadings { get; set; }

        // Thống kê sở thích
        public List<GenreStatistic> FavoriteGenreStats { get; set; } = new();
        public List<GenreStatistic> ReadingGenreStats { get; set; } = new();

        // Top sách
        public List<BookStatistic> MostFavoritedBooks { get; set; } = new();
        public List<BookStatistic> MostReadBooks { get; set; } = new();
    }

    public class GenreStatistic
    {
        public Genre Genre { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class BookStatistic
    {
        public Book Book { get; set; } = null!;
        public int FavoriteCount { get; set; }
        public int ReadCount { get; set; }
    }
}