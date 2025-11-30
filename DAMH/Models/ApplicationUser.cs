using Microsoft.AspNetCore.Identity;

namespace DAMH.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsMember { get; set; } = false;
        public DateTime? SubscriptionExpiryDate { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public ICollection<ReadingHistory> ReadingHistories { get; set; } = new List<ReadingHistory>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}