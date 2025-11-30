using System.ComponentModel.DataAnnotations;

namespace DAMH.Models
{
    public enum Genre
    {
        [Display(Name = "Viễn tưởng")]
        Fantasy = 0,
        [Display(Name = "Lãng mạn")] 
        Romance = 1,
        [Display(Name = "Trinh thám")]
        Mystery = 2,
        [Display(Name = "Khoa học")]
        ScienceFiction = 3,
        [Display(Name = "Kinh dị")] 
        Horror = 4,
        [Display(Name = "Phiêu lưu")] 
        Adventure = 5,
        [Display(Name = "Lịch sử")] 
        Historical = 6,
        [Display(Name = "Tiểu sử")] 
        Biography = 7,
        [Display(Name = "Kỹ năng sống")] 
        SelfHelp = 8,
        [Display(Name = "Giáo dục")] 
        Educational = 9
    }
}