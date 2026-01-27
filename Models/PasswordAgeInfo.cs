using System;

namespace FreshFarmMarket.Models
{
    /// <summary>
    /// Holds information about the user's password age and expiry status.
    /// This gets passed to the profile page to show password policy info.
    /// </summary>
    public class PasswordAgeInfo
    {
        // How many days since the user last changed their password
        public int DaysSinceLastChange { get; set; }

        // How many days left before the password expires (90 day max)
        public int DaysUntilExpiry { get; set; }

        // Whether the user is allowed to change their password right now
        // False if they changed it less than 5 minutes ago
        public bool CanChangePassword { get; set; }

        // If they can't change yet, how many minutes until they can
        public int MinutesUntilCanChange { get; set; }

        // True if the password has exceeded the 90 day maximum age
        public bool PasswordExpired { get; set; }

        // When the password was last changed
        public DateTime PasswordLastChanged { get; set; }

        // A helper property to show what color the expiry badge should be
        // Green = more than 7 days, Yellow = 7 days or less, Red = expired
        public string ExpiryStatusColor
        {
            get
            {
                if (PasswordExpired)
                    return "danger";  // Red
                if (DaysUntilExpiry <= 7)
                    return "warning"; // Yellow
                return "success";     // Green
            }
        }

        // Human readable expiry message for the UI
        public string ExpiryStatusMessage
        {
            get
            {
                if (PasswordExpired)
                    return "Password has expired! Please change it immediately.";
                if (DaysUntilExpiry <= 7)
                    return $"Password expires in {DaysUntilExpiry} day(s). Please change it soon.";
                return $"{DaysUntilExpiry} days until password expires.";
            }
        }
    }
}
