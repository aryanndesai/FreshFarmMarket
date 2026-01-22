using System;
using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarket.Models
{
    public class PasswordHistory
    {
        [Key]
        public int PasswordHistoryId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(500)]
        public required string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}