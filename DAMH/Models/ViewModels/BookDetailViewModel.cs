namespace DAMH.Models.ViewModels
{
    public class BookDetailViewModel
    {
        public Book Book { get; set; } = null!;
        public int? UserRating { get; set; } 
        public bool HasUserReviewed { get; set; } 
    }
}