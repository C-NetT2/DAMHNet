using Microsoft.AspNetCore.Identity;
namespace DAMH.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Custom properties for freemium logic
        public bool IsMember { get; set; } = false;

        public DateTime? SubscriptionExpiryDate { get; set; }
    }
}