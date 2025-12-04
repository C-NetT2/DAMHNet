using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DAMH.Models
{
    public partial class ApplicationUser : IdentityUser
    {
        public bool IsMember { get; set; } = false;
        public DateTime? SubscriptionExpiryDate { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public ICollection<ReadingHistory> ReadingHistories { get; set; } = new List<ReadingHistory>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}