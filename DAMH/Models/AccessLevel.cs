using System.ComponentModel.DataAnnotations;

namespace DAMH.Models
{
    public enum AccessLevel
    {
        [Display(Name = "Miễn phí")] 
        Free = 0,
        [Display(Name = "Thành viên VIP")] 
        Premium = 1
    }
}