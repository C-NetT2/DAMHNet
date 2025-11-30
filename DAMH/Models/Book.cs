using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAMH.Models
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sách")]
        [StringLength(200)]
        [Display(Name = "Tên sách")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Mô tả ngắn")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Quyền truy cập")]
        public AccessLevel AccessLevel { get; set; }

        [Required]
        [Display(Name = "Loại sách")]
        public BookType BookType { get; set; }

        [Required]
        [Display(Name = "Thể loại")]
        public Genre Genre { get; set; }

        [Required]
        [Display(Name = "Độ tuổi")]
        public AgeRating AgeRating { get; set; }

        [StringLength(5000)]
        [Display(Name = "Giới thiệu chi tiết")]
        public string? Introduction { get; set; }

        [StringLength(500)]
        [Display(Name = "Link ảnh bìa")]
        public string? CoverImageUrl { get; set; }

        [Display(Name = "Tác giả")]
        [StringLength(200)]
        public string? Author { get; set; }

        [Display(Name = "Ngày xuất bản")]
        public DateTime? PublishedDate { get; set; }

        [Display(Name = "Lượt xem")]
        public int TotalViews { get; set; } = 0;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Cập nhật cuối")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        [NotMapped]
        public double AverageRating
        {
            get
            {
                if (Reviews == null || !Reviews.Any())
                    return 0;
                return Math.Round(Reviews.Average(r => r.Rating), 1);
            }
        }

        [NotMapped]
        public int TotalReviews => Reviews?.Count ?? 0;
    }
}