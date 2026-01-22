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

        public HomeController(ApplicationDbContext context, EncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
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
            string cookieToken = Request.Cookies["AuthToken"];
            string sessionToken = HttpContext.Session.GetString("AuthToken");

            if (
                string.IsNullOrEmpty(cookieToken)
                || string.IsNullOrEmpty(sessionToken)
                || cookieToken != sessionToken
            )
            {
                // AuthToken mismatch - concurrent login or session hijacking detected
                HttpContext.Session.Clear();
                Response.Cookies.Delete("AuthToken");
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
                return RedirectToAction("Login", "Account");
            }

            // Check for session timeout (detect multiple logins)
            var sessionEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(sessionEmail) || sessionEmail != user.Email)
            {
                HttpContext.Session.Clear();
                Response.Cookies.Delete("AuthToken");
                TempData["ErrorMessage"] = "Your session has expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Decrypt sensitive data for display
            user.CreditCardNo = _encryptionService.Decrypt(user.CreditCardNo);

            return View(user);
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
