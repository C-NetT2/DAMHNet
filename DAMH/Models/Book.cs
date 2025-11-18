using System.ComponentModel.DataAnnotations;
namespace DAMH.Models
{
    public enum AccessLevel
    {
        Free = 0,
        Premium = 1
    }

    public class Book
    {
        [Key]
        public int BookId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public AccessLevel AccessLevel { get; set; }

        // Navigation property: A book can have many chapters
        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    }
}