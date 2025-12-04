using System.ComponentModel.DataAnnotations;

namespace DAMH.Models.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FullName { get; set; }

        [Phone]
        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public bool IsMember { get; set; }
        public DateTime? SubscriptionExpiryDate { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}