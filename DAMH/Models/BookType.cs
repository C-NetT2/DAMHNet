using System.ComponentModel.DataAnnotations;

namespace DAMH.Models
{
    public enum BookType
    {
        [Display(Name = "Truyện")]
        Story = 0,
        [Display(Name = "Sách")]
        Book = 1
    }
}