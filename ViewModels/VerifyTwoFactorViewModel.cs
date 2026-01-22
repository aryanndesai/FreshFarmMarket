using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarket.ViewModels
{
    public class VerifyTwoFactorViewModel
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public required string Code { get; set; }

        public required string Email { get; set; }
    }
}