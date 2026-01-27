using Microsoft.EntityFrameworkCore;
using FreshFarmMarket.Models;

namespace FreshFarmMarket.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<TwoFactorCode> TwoFactorCodes { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure email is unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Set default values
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("datetime('now')");

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordLastChanged)
                .HasDefaultValueSql("datetime('now')");

            modelBuilder.Entity<User>()
                .Property(u => u.FailedLoginAttempts)
                .HasDefaultValue(0);

            modelBuilder.Entity<User>()
                .Property(u => u.IsLocked)
                .HasDefaultValue(false);

            modelBuilder.Entity<User>()
                .Property(u => u.RequirePasswordChange)
                .HasDefaultValue(false);

            modelBuilder.Entity<User>()
                .Property(u => u.TwoFactorEnabled)
                .HasDefaultValue(true); // Enable 2FA by default

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Timestamp)
                .HasDefaultValueSql("datetime('now')");

            modelBuilder.Entity<PasswordHistory>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("datetime('now')");

            modelBuilder.Entity<PasswordResetToken>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("datetime('now')");

            modelBuilder.Entity<TwoFactorCode>()
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("datetime('now')");

            // UserSession configuration for concurrent session tracking
            modelBuilder.Entity<UserSession>()
                .Property(s => s.CreatedAt)
                .HasDefaultValueSql("datetime('now')");

            modelBuilder.Entity<UserSession>()
                .Property(s => s.LastActivityAt)
                .HasDefaultValueSql("datetime('now')");

            modelBuilder.Entity<UserSession>()
                .Property(s => s.IsActive)
                .HasDefaultValue(true);

            // Add an index on SessionToken for faster lookups during validation
            modelBuilder.Entity<UserSession>()
                .HasIndex(s => s.SessionToken)
                .IsUnique();

            // Add an index on UserId and IsActive for finding active sessions
            modelBuilder.Entity<UserSession>()
                .HasIndex(s => new { s.UserId, s.IsActive });
        }
    }
} 