namespace DAMH.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Book> HotStories { get; set; } = new(); 
        public List<Book> NewUpdates { get; set; } = new(); 
        public List<Book> TopRatedBooks { get; set; } = new(); 
    }
}