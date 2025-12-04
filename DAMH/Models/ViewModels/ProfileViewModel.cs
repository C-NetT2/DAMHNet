using System.ComponentModel.DataAnnotations;

namespace DAMH.Models.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Trạng thái VIP")]
        public bool IsMember { get; set; }

        [Display(Name = "Ngày hết hạn VIP")]
        public DateTime? SubscriptionExpiryDate { get; set; }

        public string VipStatusText => IsMember ? "Đang kích hoạt" : "Chưa kích hoạt";

        public string VipExpiryText
        {
            get
            {
                if (!IsMember || SubscriptionExpiryDate == null)
                    return "Không áp dụng";

                var daysLeft = (SubscriptionExpiryDate.Value - DateTime.Now).Days;
                if (daysLeft < 0) return "Đã hết hạn";
                if (daysLeft == 0) return "Hết hạn hôm nay";
                return $"Còn {daysLeft} ngày";
            }
        }

        [Display(Name = "Họ và tên")]
        [StringLength(100)]
        public string? FullName { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone]
        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(500)]
        public string? Address { get; set; }

        [Display(Name = "Ngày đăng ký")]
        public DateTime RegistrationDate { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}