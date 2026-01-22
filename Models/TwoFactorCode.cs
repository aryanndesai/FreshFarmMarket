using System;
using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarket.Models
{
    public class TwoFactorCode
    {
        [Key]
        public int TwoFactorCodeId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(6)]
        public required string Code { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsUsed { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}