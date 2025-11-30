using System.ComponentModel.DataAnnotations;

namespace DAMH.Models
{
    public enum AgeRating
    {
        [Display(Name = "Mọi lứa tuổi")] 
        AllAges = 0,
        [Display(Name = "Trên 13 tuổi")] 
        Teen13Plus = 1,
        [Display(Name = "Trên 16 tuổi")] 
        Teen16Plus = 2,
        [Display(Name = "Trên 18 tuổi")]
        Adult18Plus = 3
    }
}