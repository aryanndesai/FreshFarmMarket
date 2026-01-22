using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace FreshFarmMarket.Services
{
    public class PasswordService
    {
        // Hash password using PBKDF2 with high iteration count (2025 standard)
        public string HashPassword(string password)
        {
            // Generate a 128-bit salt using a secure PRNG
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password with PBKDF2 (600,000 iterations - OWASP 2023 recommendation)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 600000,
                numBytesRequested: 256 / 8));

            // Combine salt and hash for storage
            return $"{Convert.ToBase64String(salt)}:{hashed}";
        }

        // Verify password against stored hash
        public bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split(':');
                if (parts.Length != 2)
                    return false;

                var salt = Convert.FromBase64String(parts[0]);
                var hash = parts[1];

                // Hash the provided password with the same salt
                string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 600000,
                    numBytesRequested: 256 / 8));

                // Compare hashes
                return hash == hashedPassword;
            }
            catch
            {
                return false;
            }
        }

        // Generate secure random token for password reset
        public string GenerateSecureToken()
        {
            byte[] tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        // Generate 6-digit code for 2FA
        public string GenerateTwoFactorCode()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                int number = Math.Abs(BitConverter.ToInt32(bytes, 0));
                return (number % 1000000).ToString("D6");
            }
        }
    }
}