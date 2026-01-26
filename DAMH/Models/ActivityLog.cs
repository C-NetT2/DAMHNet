using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAMH.Models
{
    public class ActivityLog
    {
        [Key]
        public int ActivityLogId { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Hành động")]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Phần")]
        public string Section { get; set; } = string.Empty; 

        [StringLength(100)]
        [Display(Name = "ID đối tượng")]
        public string? EntityId { get; set; }

        [Display(Name = "Tên đối tượng")]
        [StringLength(500)]
        public string? EntityName { get; set; }

        [Display(Name = "Nội dung cũ")]
        public string? OldValues { get; set; }

        [Display(Name = "Nội dung mới")]
        public string? NewValues { get; set; }

        [StringLength(1000)]
        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        [Required]
        [Display(Name = "Thời gian")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(45)]
        [Display(Name = "Địa chỉ IP")]
        public string? IpAddress { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;
    }
}