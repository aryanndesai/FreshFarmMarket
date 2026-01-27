using System.Diagnostics;
using FreshFarmMarket.Data;
using FreshFarmMarket.Models;
using FreshFarmMarket.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreshFarmMarket.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryptionService;
        private readonly SessionManagementService _sessionService;

        // Password policy constants (keep in sync with AccountController)
        private const int MIN_PASSWORD_AGE_MINUTES = 5;
        private const int MAX_PASSWORD_AGE_DAYS = 90;

        public HomeController(
            ApplicationDbContext context, 
            EncryptionService encryptionService,
            SessionManagementService sessionService)
        {
            _context = context;
            _encryptionService = encryptionService;
            _sessionService = sessionService;
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Validate AuthToken - detect concurrent logins or session hijacking
            string? cookieToken = Request.Cookies["AuthToken"];
            string? sessionToken = HttpContext.Session.GetString("AuthToken");

            if (
                string.IsNullOrEmpty(cookieToken)
                || string.IsNullOrEmpty(sessionToken)
                || cookieToken != sessionToken
            )
            {
                // AuthToken mismatch - concurrent login or session hijacking detected
                HttpContext.Session.Clear();
                Response.Cookies.Delete("AuthToken");
                Response.Cookies.Delete("SessionToken");
                TempData["ErrorMessage"] =
                    "Your session has expired or you logged in from another device. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Get user data
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                HttpContext.Session.Clear();
                Response.Cookies.Delete("AuthToken");
                Response.Cookies.Delete("SessionToken");
                return RedirectToAction("Login", "Account");
            }

            // Check for session timeout (detect multiple logins)
            var sessionEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(sessionEmail) || sessionEmail != user.Email)
            {
                HttpContext.Session.Clear();
                Response.Cookies.Delete("AuthToken");
                Response.Cookies.Delete("SessionToken");
                TempData["ErrorMessage"] = "Your session has expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Decrypt sensitive data for display
            user.CreditCardNo = _encryptionService.Decrypt(user.CreditCardNo);

            // Calculate password age info for the view
            var passwordAgeInfo = GetPasswordAgeInfo(user);
            ViewBag.PasswordAgeInfo = passwordAgeInfo;

            // Get session info for the view
            var dbSessionToken = Request.Cookies["SessionToken"];
            var currentSession = await _sessionService.GetSessionByTokenAsync(dbSessionToken);
            var activeSessionCount = await _sessionService.GetActiveSessionCountAsync(userId.Value);
            ViewBag.CurrentSession = currentSession;
            ViewBag.ActiveSessionCount = activeSessionCount;

            return View(user);
        }

        /// <summary>
        /// Calculates the password age information for displaying on the profile page.
        /// This tells the user how old their password is and when it will expire.
        /// </summary>
        private PasswordAgeInfo GetPasswordAgeInfo(User user)
        {
            var now = DateTime.UtcNow;
            var timeSinceLastChange = now - user.PasswordLastChanged;
            var daysSinceLastChange = (int)timeSinceLastChange.TotalDays;
            var minutesSinceLastChange = timeSinceLastChange.TotalMinutes;

            // Calculate how many days until the password expires (90 day max)
            var daysUntilExpiry = MAX_PASSWORD_AGE_DAYS - daysSinceLastChange;
            if (daysUntilExpiry < 0) daysUntilExpiry = 0;

            // Check if the user can change their password (5 minute minimum age)
            var canChangePassword = minutesSinceLastChange >= MIN_PASSWORD_AGE_MINUTES;
            var minutesUntilCanChange = 0;
            if (!canChangePassword)
            {
                minutesUntilCanChange = (int)Math.Ceiling(MIN_PASSWORD_AGE_MINUTES - minutesSinceLastChange);
            }

            return new PasswordAgeInfo
            {
                DaysSinceLastChange = daysSinceLastChange,
                DaysUntilExpiry = daysUntilExpiry,
                CanChangePassword = canChangePassword,
                MinutesUntilCanChange = minutesUntilCanChange,
                PasswordExpired = daysSinceLastChange >= MAX_PASSWORD_AGE_DAYS,
                PasswordLastChanged = user.PasswordLastChanged
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                }
            );
        }
    }
}
