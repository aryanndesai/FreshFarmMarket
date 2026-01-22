using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FreshFarmMarket.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Full Name can only contain letters and spaces")]
        [Display(Name = "Full Name")]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Credit Card Number is required")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Credit Card must be exactly 16 digits")]
        [Display(Name = "Credit Card Number")]
        public required string CreditCardNo { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public required string Gender { get; set; }

        [Required(ErrorMessage = "Mobile Number is required")]
        [RegularExpression(@"^[89]\d{7}$", ErrorMessage = "Mobile Number must be a valid Singapore number (8 digits starting with 8 or 9)")]
        [Display(Name = "Mobile Number")]
        public required string MobileNo { get; set; }

        [Required(ErrorMessage = "Delivery Address is required")]
        [StringLength(500, ErrorMessage = "Delivery Address cannot exceed 500 characters")]
        [Display(Name = "Delivery Address")]
        public required string DeliveryAddress { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#^()_+=\-\[\]{}|\\:;""'<>,.~/`])[A-Za-z\d@$!%*?&#^()_+=\-\[\]{}|\\:;""'<>,.~/`]{12,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public required string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Photo is required")]
        [Display(Name = "Photo (.JPG only)")]
        public required IFormFile Photo { get; set; }

        [Display(Name = "About Me")]
        [StringLength(1000, ErrorMessage = "About Me cannot exceed 1000 characters")]
        public required string AboutMe { get; set; }
    }
}