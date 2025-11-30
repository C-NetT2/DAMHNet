using System.ComponentModel.DataAnnotations;

namespace DAMH.Models
{
    public enum MediaType
    {
        [Display(Name = "Hình ảnh")]
        Image = 0,
        [Display(Name = "Video")]
        Video = 1
    }
}