using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAMH.Models
{
    public class ReadingHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int BookId { get; set; }

        [Required]
        public int ChapterId { get; set; }

        [Required]
        public DateTime AccessTime { get; set; } = DateTime.Now;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [ForeignKey(nameof(BookId))]
        public Book Book { get; set; } = null!;

        [ForeignKey(nameof(ChapterId))]
        public Chapter Chapter { get; set; } = null!;
    }
}