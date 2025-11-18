using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DAMH.Models
{
    public class Chapter
    {
        [Key]
        public int ChapterId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        [Required]
        public int ChapterOrder { get; set; }

        [Required]
        public bool IsFree { get; set; }

        // Foreign key
        [Required]
        public int BookId { get; set; }

        // Navigation property: A chapter belongs to one book
        [ForeignKey(nameof(BookId))]
        public Book Book { get; set; } = null!;
    }
}
