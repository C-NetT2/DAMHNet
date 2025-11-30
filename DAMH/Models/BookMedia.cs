using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAMH.Models
{
    public class BookMedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Đường dẫn (URL)")]
        public string Url { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Loại phương tiện")]
        public MediaType MediaType { get; set; }

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [ForeignKey(nameof(BookId))]
        public Book Book { get; set; } = null!;
    }
}