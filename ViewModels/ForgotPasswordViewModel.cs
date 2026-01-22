using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarket.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public required string Email { get; set; }
    }
}