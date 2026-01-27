using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreshFarmMarket.Data;
using FreshFarmMarket.Models;
using Microsoft.EntityFrameworkCore;

namespace FreshFarmMarket.Services
{
    /// <summary>
    /// Handles all the session tracking logic to enforce single active session per user.
    /// When someone logs in from a new device, this service terminates their old sessions.
    /// </summary>
    public class SessionManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public SessionManagementService(ApplicationDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        /// <summary>
        /// Creates a new session for the user and kills all their previous sessions.
        /// This is the main method called during login.
        /// </summary>
        /// <param name="userId">The ID of the user logging in</param>
        /// <param name="ipAddress">The IP address they're logging in from</param>
        /// <param name="userAgent">Their browser info from the User-Agent header</param>
        /// <returns>The new session token to store in the user's cookie</returns>
        public async Task<string> CreateSessionAsync(int userId, string? ipAddress, string? userAgent)
        {
            // First, terminate any existing active sessions for this user
            // This is what enforces the "one session at a time" rule
            var existingActiveSessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            if (existingActiveSessions.Any())
            {
                // Log that we detected a concurrent login attempt
                await _auditService.LogAsync(
                    "Concurrent Login Detected",
                    userId,
                    $"Terminating {existingActiveSessions.Count} existing session(s) due to new login from IP: {ipAddress}",
                    true
                );

                // Mark all old sessions as inactive
                foreach (var session in existingActiveSessions)
                {
                    session.IsActive = false;
                    session.LoggedOutAt = DateTime.UtcNow;
                    session.LogoutReason = "Logged in from another device";
                }
            }

            // Generate a new unique session token
            // This is what we'll check on every request to validate the session
            var sessionToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            // Create the new session record
            var newSession = new UserSession
            {
                UserId = userId,
                SessionToken = sessionToken,
                IpAddress = TruncateString(ipAddress, 50),
                UserAgent = TruncateString(userAgent, 500),
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsActive = true,
                LoggedOutAt = null,
                LogoutReason = null
            };

            _context.UserSessions.Add(newSession);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                "Session Created",
                userId,
                $"New session created from IP: {ipAddress}",
                true
            );

            return sessionToken;
        }

        /// <summary>
        /// Checks if a session token is still valid.
        /// Returns false if the session was terminated (like if the user logged in elsewhere).
        /// </summary>
        /// <param name="sessionToken">The token from the user's cookie</param>
        /// <returns>True if the session is still active, false otherwise</returns>
        public async Task<bool> ValidateSessionAsync(string? sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken))
            {
                return false;
            }

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (session == null)
            {
                return false;
            }

            // Check if the session is still active
            if (!session.IsActive)
            {
                return false;
            }

            // Update the last activity timestamp so we know the session is still being used
            session.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Gets the user ID associated with a session token.
        /// Returns null if the session doesn't exist or is inactive.
        /// </summary>
        public async Task<int?> GetUserIdFromSessionAsync(string? sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken))
            {
                return null;
            }

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

            return session?.UserId;
        }

        /// <summary>
        /// Terminates a specific session by its token.
        /// Called when the user explicitly logs out.
        /// </summary>
        public async Task TerminateSessionAsync(string? sessionToken, string reason = "User logged out")
        {
            if (string.IsNullOrEmpty(sessionToken))
            {
                return;
            }

            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (session != null && session.IsActive)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
                session.LogoutReason = reason;
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    "Session Terminated",
                    session.UserId,
                    $"Session ended: {reason}",
                    true
                );
            }
        }

        /// <summary>
        /// Terminates all active sessions for a user.
        /// Useful for security events like password changes.
        /// </summary>
        public async Task TerminateAllSessionsAsync(int userId, string reason = "All sessions terminated")
        {
            var activeSessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
                session.LogoutReason = reason;
            }

            if (activeSessions.Any())
            {
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(
                    "All Sessions Terminated",
                    userId,
                    $"{activeSessions.Count} session(s) terminated: {reason}",
                    true
                );
            }
        }

        /// <summary>
        /// Gets all active sessions for a user.
        /// Used to display session info on the profile page.
        /// </summary>
        public async Task<List<UserSession>> GetActiveSessionsAsync(int userId)
        {
            return await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Gets the count of active sessions for a user.
        /// Should normally be 1 since we only allow one session at a time.
        /// </summary>
        public async Task<int> GetActiveSessionCountAsync(int userId)
        {
            return await _context.UserSessions
                .CountAsync(s => s.UserId == userId && s.IsActive);
        }

        /// <summary>
        /// Gets the current active session for a user by their token.
        /// Used to show session details on the profile page.
        /// </summary>
        public async Task<UserSession?> GetSessionByTokenAsync(string? sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken))
            {
                return null;
            }

            return await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);
        }

        /// <summary>
        /// Helper method to truncate strings to fit in database columns.
        /// Prevents errors when IP addresses or user agents are too long.
        /// </summary>
        private static string? TruncateString(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }
    }
}
