namespace DAMH.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Book> HotStories { get; set; } = new(); // Truyện Hot
        public List<Book> NewUpdates { get; set; } = new(); // Mới cập nhật
        public List<Book> TopRatedBooks { get; set; } = new(); // BXH Đánh giá
    }
}