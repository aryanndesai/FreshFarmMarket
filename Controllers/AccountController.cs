using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FreshFarmMarket.Data;
using FreshFarmMarket.Models;
using FreshFarmMarket.ViewModels;
using FreshFarmMarket.Services;
using Microsoft.AspNetCore.Http;

namespace FreshFarmMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryptionService;
        private readonly AuditService _auditService;
        private readonly PasswordService _passwordService;
        private readonly EmailService _emailService;

        // Account lockout settings
        private const int MAX_FAILED_ATTEMPTS = 3;
        private const int LOCKOUT_DURATION_MINUTES = 5;
        private const int AUTO_UNLOCK_MINUTES = 10;
        
        // Password policy settings
        private const int MIN_PASSWORD_AGE_MINUTES = 1; // Minimum time before can change password again
        private const int MAX_PASSWORD_AGE_DAYS = 90; // Force password change after 90 days
        private const int PASSWORD_HISTORY_COUNT = 2; // Remember last 2 passwords

        public AccountController(
            ApplicationDbContext context,
            EncryptionService encryptionService,
            AuditService auditService,
            PasswordService passwordService,
            EmailService emailService)
        {
            _context = context;
            _encryptionService = encryptionService;
            _auditService = auditService;
            _passwordService = passwordService;
            _emailService = emailService;
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            // If already logged in, redirect to home
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check for duplicate email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email address is already registered");
                await _auditService.LogAsync("Registration Failed", null, $"Duplicate email: {model.Email}", false);
                return View(model);
            }

            // Validate photo upload (must be .jpg)
            if (model.Photo == null || model.Photo.Length == 0)
            {
                ModelState.AddModelError("Photo", "Please upload a photo");
                return View(model);
            }

            var extension = Path.GetExtension(model.Photo.FileName).ToLower();
            if (extension != ".jpg" && extension != ".jpeg")
            {
                ModelState.AddModelError("Photo", "Only .JPG files are allowed");
                return View(model);
            }

            // Validate file size (max 5MB)
            if (model.Photo.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("Photo", "File size must not exceed 5MB");
                return View(model);
            }

            // Save photo to wwwroot/uploads
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{model.Photo.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.Photo.CopyToAsync(fileStream);
            }

            // Hash password
            var passwordHash = _passwordService.HashPassword(model.Password);

            // Encrypt sensitive data
            var encryptedCreditCard = _encryptionService.Encrypt(model.CreditCardNo);

            // Create new user
            var user = new User
            {
                FullName = model.FullName,
                CreditCardNo = encryptedCreditCard,
                Gender = model.Gender,
                MobileNo = model.MobileNo,
                DeliveryAddress = model.DeliveryAddress,
                Email = model.Email.ToLower(),
                PasswordHash = passwordHash,
                PhotoPath = $"/uploads/{uniqueFileName}",
                AboutMe = model.AboutMe,
                CreatedAt = DateTime.UtcNow,
                PasswordLastChanged = DateTime.UtcNow,
                FailedLoginAttempts = 0,
                IsLocked = false,
                RequirePasswordChange = false,
                TwoFactorEnabled = true // Enable 2FA by default
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Save password history
            var passwordHistory = new PasswordHistory
            {
                UserId = user.UserId,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };
            _context.PasswordHistories.Add(passwordHistory);
            await _context.SaveChangesAsync();

            // Log registration
            await _auditService.LogAsync("User Registration", user.UserId, $"New user registered: {user.Email}", true);

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);

            TempData["SuccessMessage"] = "Registration successful! Please log in.";
            return RedirectToAction("Login");
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to home
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                await _auditService.LogAsync("Login Failed", null, $"User not found: {model.Email}", false);
                return View(model);
            }

            // Check if account is locked
            if (user.IsLocked)
            {
                // Check if auto-unlock time has passed
                if (user.LockedUntil.HasValue && DateTime.UtcNow >= user.LockedUntil.Value)
                {
                    // Auto-unlock account
                    user.IsLocked = false;
                    user.FailedLoginAttempts = 0;
                    user.LockedUntil = null;
                    await _context.SaveChangesAsync();
                    await _auditService.LogAsync("Account Auto-Unlocked", user.UserId, $"Account automatically unlocked: {user.Email}", true);
                }
                else
                {
                    var remainingTime = user.LockedUntil!.Value - DateTime.UtcNow;
                    ModelState.AddModelError("", $"Account is locked. Try again in {Math.Ceiling(remainingTime.TotalMinutes)} minutes.");
                    await _auditService.LogAsync("Login Failed", user.UserId, $"Attempted login on locked account: {user.Email}", false);
                    return View(model);
                }
            }

            // Verify password
            if (!_passwordService.VerifyPassword(model.Password, user.PasswordHash))
            {
                // Increment failed login attempts
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= MAX_FAILED_ATTEMPTS)
                {
                    // Lock account
                    user.IsLocked = true;
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(AUTO_UNLOCK_MINUTES);
                    await _context.SaveChangesAsync();

                    ModelState.AddModelError("", $"Account locked due to {MAX_FAILED_ATTEMPTS} failed login attempts. Try again in {AUTO_UNLOCK_MINUTES} minutes.");
                    await _auditService.LogAsync("Account Locked", user.UserId, $"Account locked after {MAX_FAILED_ATTEMPTS} failed attempts: {user.Email}", false);
                }
                else
                {
                    await _context.SaveChangesAsync();
                    var attemptsLeft = MAX_FAILED_ATTEMPTS - user.FailedLoginAttempts;
                    ModelState.AddModelError("", $"Invalid email or password. {attemptsLeft} attempt(s) remaining.");
                    await _auditService.LogAsync("Login Failed", user.UserId, $"Invalid password: {user.Email}", false);
                }

                return View(model);
            }

            // Reset failed login attempts on successful password verification
            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            // Check if password change is required (max age exceeded)
            var daysSincePasswordChange = (DateTime.UtcNow - user.PasswordLastChanged).TotalDays;
            if (daysSincePasswordChange > MAX_PASSWORD_AGE_DAYS)
            {
                user.RequirePasswordChange = true;
                await _context.SaveChangesAsync();
                TempData["WarningMessage"] = $"Your password is {(int)daysSincePasswordChange} days old. Please change it.";
                HttpContext.Session.SetInt32("TempUserId", user.UserId);
                return RedirectToAction("ChangePassword");
            }

            // If 2FA is enabled, send verification code
            if (user.TwoFactorEnabled)
            {
                // Generate and save 2FA code
                var twoFactorCode = _passwordService.GenerateTwoFactorCode();
                var codeExpiry = DateTime.UtcNow.AddMinutes(5); // 5 minute expiry

                var tfaCode = new TwoFactorCode
                {
                    UserId = user.UserId,
                    Code = twoFactorCode,
                    ExpiryDate = codeExpiry,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TwoFactorCodes.Add(tfaCode);
                await _context.SaveChangesAsync();

                // Send code via email
                await _emailService.SendTwoFactorCodeAsync(user.Email, twoFactorCode);

                // Store email in TempData for 2FA verification
                TempData["TwoFactorEmail"] = user.Email;

                await _auditService.LogAsync("2FA Code Sent", user.UserId, $"Two-factor code sent to: {user.Email}", true);

                return RedirectToAction("VerifyTwoFactor");
            }

            // Complete login (if 2FA disabled)
            await CompleteLogin(user);

            return RedirectToAction("Index", "Home");
        }

        // Complete login process (called after 2FA or direct login)
        private async Task CompleteLogin(User user)
        {
            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Create session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.FullName);

            // Log successful login
            await _auditService.LogAsync("Login Successful", user.UserId, $"User logged in: {user.Email}", true);
        }

        // GET: Account/VerifyTwoFactor
        [HttpGet]
        public IActionResult VerifyTwoFactor()
        {
            var email = TempData["TwoFactorEmail"] as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            // Keep email for POST request
            TempData.Keep("TwoFactorEmail");

            return View(new VerifyTwoFactorViewModel 
            { 
                Email = email,
                Code = string.Empty 
            });
        }

        // POST: Account/VerifyTwoFactor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid verification attempt");
                return View(model);
            }

            // Find valid code for this user
            var validCode = await _context.TwoFactorCodes
                .Where(t => t.UserId == user.UserId && 
                           t.Code == model.Code && 
                           !t.IsUsed && 
                           t.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (validCode == null)
            {
                ModelState.AddModelError("", "Invalid or expired verification code");
                await _auditService.LogAsync("2FA Failed", user.UserId, $"Invalid 2FA code entered: {user.Email}", false);
                return View(model);
            }

            // Mark code as used
            validCode.IsUsed = true;
            await _context.SaveChangesAsync();

            // Complete login
            await CompleteLogin(user);

            await _auditService.LogAsync("2FA Verified", user.UserId, $"Two-factor authentication successful: {user.Email}", true);

            TempData["SuccessMessage"] = "Login successful!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ResendTwoFactorCode
        [HttpGet]
        public async Task<IActionResult> ResendTwoFactorCode()
        {
            var email = TempData["TwoFactorEmail"] as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Generate new code
            var twoFactorCode = _passwordService.GenerateTwoFactorCode();
            var codeExpiry = DateTime.UtcNow.AddMinutes(5);

            var tfaCode = new TwoFactorCode
            {
                UserId = user.UserId,
                Code = twoFactorCode,
                ExpiryDate = codeExpiry,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.TwoFactorCodes.Add(tfaCode);
            await _context.SaveChangesAsync();

            await _emailService.SendTwoFactorCodeAsync(user.Email, twoFactorCode);

            TempData["TwoFactorEmail"] = email;
            TempData["SuccessMessage"] = "New verification code sent to your email";

            return RedirectToAction("VerifyTwoFactor");
        }

        // GET: Account/ChangePassword
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? HttpContext.Session.GetInt32("TempUserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.RequirePasswordChange)
            {
                ViewBag.RequiredChange = true;
            }

            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = HttpContext.Session.GetInt32("UserId") ?? HttpContext.Session.GetInt32("TempUserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Verify current password
            if (!_passwordService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                await _auditService.LogAsync("Password Change Failed", user.UserId, "Incorrect current password", false);
                return View(model);
            }

            // Check minimum password age
            var minutesSinceLastChange = (DateTime.UtcNow - user.PasswordLastChanged).TotalMinutes;
            if (minutesSinceLastChange < MIN_PASSWORD_AGE_MINUTES && !user.RequirePasswordChange)
            {
                ModelState.AddModelError("", $"You can only change your password once every {MIN_PASSWORD_AGE_MINUTES} minute(s)");
                return View(model);
            }

            // Check password history (prevent reuse)
            var passwordHistories = await _context.PasswordHistories
                .Where(p => p.UserId == user.UserId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(PASSWORD_HISTORY_COUNT)
                .ToListAsync();

            foreach (var history in passwordHistories)
            {
                if (_passwordService.VerifyPassword(model.NewPassword, history.PasswordHash))
                {
                    ModelState.AddModelError("NewPassword", $"You cannot reuse your last {PASSWORD_HISTORY_COUNT} passwords");
                    await _auditService.LogAsync("Password Change Failed", user.UserId, "Password reuse attempted", false);
                    return View(model);
                }
            }

            // Hash new password
            var newPasswordHash = _passwordService.HashPassword(model.NewPassword);

            // Update user password
            user.PasswordHash = newPasswordHash;
            user.PasswordLastChanged = DateTime.UtcNow;
            user.RequirePasswordChange = false;
            await _context.SaveChangesAsync();

            // Save to password history
            var newHistory = new PasswordHistory
            {
                UserId = user.UserId,
                PasswordHash = newPasswordHash,
                CreatedAt = DateTime.UtcNow
            };
            _context.PasswordHistories.Add(newHistory);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Password Changed", user.UserId, "Password successfully changed", true);

            // If this was a required change, complete login
            if (HttpContext.Session.GetInt32("TempUserId") != null)
            {
                HttpContext.Session.Remove("TempUserId");
                await CompleteLogin(user);
                TempData["SuccessMessage"] = "Password changed successfully! You are now logged in.";
                return RedirectToAction("Index", "Home");
            }

            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            // Always show success message (security best practice - don't reveal if email exists)
            TempData["SuccessMessage"] = "If an account exists with this email, a password reset link has been sent.";

            if (user != null)
            {
                // Generate reset token
                var resetToken = _passwordService.GenerateSecureToken();
                var tokenExpiry = DateTime.UtcNow.AddHours(1); // 1 hour expiry

                var passwordResetToken = new PasswordResetToken
                {
                    UserId = user.UserId,
                    Token = resetToken,
                    ExpiryDate = tokenExpiry,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PasswordResetTokens.Add(passwordResetToken);
                await _context.SaveChangesAsync();

                // Generate reset link
                var resetLink = Url.Action("ResetPassword", "Account", 
                    new { token = resetToken, email = user.Email }, 
                    protocol: Request.Scheme);

                // Send reset email
                if (resetLink != null)
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
                }

                await _auditService.LogAsync("Password Reset Requested", user.UserId, $"Reset token sent to: {user.Email}", true);
            }

            return RedirectToAction("Login");
        }

        // GET: Account/ResetPassword
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid password reset link";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid password reset link";
                return RedirectToAction("Login");
            }

            var validToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.UserId && 
                                         t.Token == token && 
                                         !t.IsUsed && 
                                         t.ExpiryDate > DateTime.UtcNow);

            if (validToken == null)
            {
                TempData["ErrorMessage"] = "This password reset link has expired or is invalid";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email,
                NewPassword = string.Empty,
                ConfirmNewPassword = string.Empty
            };

            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid reset request");
                return View(model);
            }

            var validToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.UserId && 
                                         t.Token == model.Token && 
                                         !t.IsUsed && 
                                         t.ExpiryDate > DateTime.UtcNow);

            if (validToken == null)
            {
                ModelState.AddModelError("", "This password reset link has expired or is invalid");
                return View(model);
            }

            // Check password history (prevent reuse)
            var passwordHistories = await _context.PasswordHistories
                .Where(p => p.UserId == user.UserId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(PASSWORD_HISTORY_COUNT)
                .ToListAsync();

            foreach (var history in passwordHistories)
            {
                if (_passwordService.VerifyPassword(model.NewPassword, history.PasswordHash))
                {
                    ModelState.AddModelError("NewPassword", $"You cannot reuse your last {PASSWORD_HISTORY_COUNT} passwords");
                    return View(model);
                }
            }

            // Hash new password
            var newPasswordHash = _passwordService.HashPassword(model.NewPassword);

            // Update user password
            user.PasswordHash = newPasswordHash;
            user.PasswordLastChanged = DateTime.UtcNow;
            user.RequirePasswordChange = false;
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            user.LockedUntil = null;
            await _context.SaveChangesAsync();

            // Mark token as used
            validToken.IsUsed = true;
            await _context.SaveChangesAsync();

            // Save to password history
            var newHistory = new PasswordHistory
            {
                UserId = user.UserId,
                PasswordHash = newPasswordHash,
                CreatedAt = DateTime.UtcNow
            };
            _context.PasswordHistories.Add(newHistory);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Password Reset", user.UserId, "Password successfully reset", true);

            TempData["SuccessMessage"] = "Password reset successful! Please log in with your new password.";
            return RedirectToAction("Login");
        }

        // GET: Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            
            if (userId.HasValue)
            {
                await _auditService.LogAsync("Logout", userId.Value, "User logged out", true);
            }

            // Clear all session data
            HttpContext.Session.Clear();

            TempData["SuccessMessage"] = "You have been logged out successfully";
            return RedirectToAction("Login");
        }
    }
}