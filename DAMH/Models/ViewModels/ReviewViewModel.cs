using System.ComponentModel.DataAnnotations;

namespace DAMH.Models.ViewModels
{
    public class ReviewViewModel
    {
        [Required]
        public int BookId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Vui lòng chọn từ 1 đến 5 sao")]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }
}