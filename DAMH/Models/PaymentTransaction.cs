using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAMH.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public VipPackageType PackageType { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Completed";

        [StringLength(500)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;
    }

    public enum VipPackageType
    {
        [Display(Name = "1 Tháng - 50,000 VND")]
        OneMonth = 1,

        [Display(Name = "3 Tháng - 130,000 VND")]
        ThreeMonths = 3,

        [Display(Name = "6 Tháng - 250,000 VND")]
        SixMonths = 6,

        [Display(Name = "1 Năm - 450,000 VND")]
        OneYear = 12,

        [Display(Name = "Trọn đời - 1,200,000 VND")]
        Lifetime = 999
    }

    public static class VipPackageHelper
    {
        public static decimal GetPrice(VipPackageType package)
        {
            return package switch
            {
                VipPackageType.OneMonth => 50000m,
                VipPackageType.ThreeMonths => 130000m,
                VipPackageType.SixMonths => 250000m,
                VipPackageType.OneYear => 450000m,
                VipPackageType.Lifetime => 1200000m,
                _ => 0m
            };
        }

        public static int GetMonths(VipPackageType package)
        {
            return (int)package;
        }

        public static DateTime CalculateExpiryDate(DateTime? currentExpiry, VipPackageType package)
        {
            var startDate = currentExpiry > DateTime.Now ? currentExpiry.Value : DateTime.Now;

            if (package == VipPackageType.Lifetime)
            {
                return DateTime.Now.AddYears(100);
            }

            return startDate.AddMonths(GetMonths(package));
        }
    }
}