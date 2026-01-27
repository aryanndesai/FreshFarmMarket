using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreshFarmMarket.Models
{
    /// <summary>
    /// Tracks user login sessions to prevent concurrent logins.
    /// Each time a user logs in, we create a new session and invalidate old ones.
    /// This ensures only one active session per user at any time.
    /// </summary>
    public class UserSession
    {
        [Key]
        public int SessionId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        // A unique token we generate for each session
        // This gets stored in the user's cookie and validated on each request
        [Required]
        [StringLength(100)]
        public required string SessionToken { get; set; }

        // The IP address where the user logged in from
        // Helps with security auditing and showing session info
        [StringLength(50)]
        public string? IpAddress { get; set; }

        // Browser and device info from the User-Agent header
        // Shows the user what device their sessions are on
        [StringLength(500)]
        public string? UserAgent { get; set; }

        // When this session was created (login time)
        public DateTime CreatedAt { get; set; }

        // Last time we saw activity from this session
        // Could be used for idle timeout tracking
        public DateTime LastActivityAt { get; set; }

        // Whether this session is still valid
        // Gets set to false when user logs out or logs in from another device
        public bool IsActive { get; set; }

        // When this session was terminated (if it was)
        // Null means the session is still active
        public DateTime? LoggedOutAt { get; set; }

        // Why this session ended, like "User logged out" or "Logged in from another device"
        [StringLength(200)]
        public string? LogoutReason { get; set; }
    }
}
