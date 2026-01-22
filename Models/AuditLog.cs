using System;
using System.ComponentModel.DataAnnotations;

namespace FreshFarmMarket.Models
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }

        public int? UserId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Action { get; set; }

        [StringLength(500)]
        public required string Details { get; set; }

        [StringLength(50)]
        public required string IpAddress { get; set; }

        public DateTime Timestamp { get; set; }

        public bool Success { get; set; }
    }
}