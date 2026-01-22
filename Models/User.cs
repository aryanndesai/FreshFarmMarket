using System;
using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarket.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public required string FullName { get; set; }

        [Required]
        [StringLength(500)]
        public required string CreditCardNo { get; set; } // Will be encrypted

        [Required]
        [StringLength(10)]
        public required string Gender { get; set; }

        [Required]
        [StringLength(20)]
        [Phone]
        public required string MobileNo { get; set; }

        [Required]
        [StringLength(500)]
        public required string DeliveryAddress { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public required string Email { get; set; }

        [Required]
        [StringLength(500)]
        public required string PasswordHash { get; set; }

        [StringLength(255)]
        public required string PhotoPath { get; set; }

        public required string AboutMe { get; set; } // Allows special chars

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLogin { get; set; }

        public int FailedLoginAttempts { get; set; }

        public DateTime? LockedUntil { get; set; }

        public bool IsLocked { get; set; }

        // Advanced password policy fields
        public DateTime PasswordLastChanged { get; set; }

        public bool RequirePasswordChange { get; set; }

        public bool TwoFactorEnabled { get; set; }
    }
}