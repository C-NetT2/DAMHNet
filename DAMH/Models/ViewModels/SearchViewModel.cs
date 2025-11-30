namespace DAMH.Models.ViewModels
{
    public class SearchViewModel
    {
        public string? Keyword { get; set; }
        public BookType? BookType { get; set; }
        public Genre? Genre { get; set; }
        public AgeRating? AgeRating { get; set; }
    }
}