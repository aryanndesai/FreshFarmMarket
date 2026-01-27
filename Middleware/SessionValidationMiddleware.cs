using System;
using System.Threading.Tasks;
using FreshFarmMarket.Services;
using Microsoft.AspNetCore.Http;

namespace FreshFarmMarket.Middleware
{
    /// <summary>
    /// This middleware runs on every request to check if the user's session is still valid.
    /// If someone logged in from another device, this will detect it and kick them out.
    /// </summary>
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        // Pages that don't require session validation
        // Basically login, register, and password reset stuff
        private static readonly string[] PublicPaths = new[]
        {
            "/account/login",
            "/account/register",
            "/account/forgotpassword",
            "/account/resetpassword",
            "/account/verifytwofactor",
            "/account/resendtwofactorcode",
            "/error"
        };

        public SessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, SessionManagementService sessionService)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Skip validation for public pages and static files
            if (IsPublicPath(path) || IsStaticFile(path))
            {
                await _next(context);
                return;
            }

            // Check if the user has a session
            var userId = context.Session.GetInt32("UserId");
            if (userId == null)
            {
                // No session means they're not logged in, let the controller handle it
                await _next(context);
                return;
            }

            // Get the session token from the cookie
            var sessionToken = context.Request.Cookies["SessionToken"];

            // Validate the session token against the database
            var isValidSession = await sessionService.ValidateSessionAsync(sessionToken);

            if (!isValidSession)
            {
                // Session is no longer valid, probably because they logged in from another device
                // Clear everything and redirect them to login with a message
                context.Session.Clear();

                // Delete all auth-related cookies
                context.Response.Cookies.Delete("SessionToken");
                context.Response.Cookies.Delete("AuthToken");

                // For AJAX requests, return a 401 status
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new 
                    { 
                        error = "Session terminated",
                        message = "Your session has been terminated because you logged in from another device."
                    });
                    return;
                }

                // Store the message in a cookie since TempData won't work here
                context.Response.Cookies.Append("SessionTerminatedMessage", 
                    "Your session has been terminated because you logged in from another device.",
                    new CookieOptions
                    {
                        Expires = DateTime.Now.AddMinutes(5),
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });

                // Redirect to login page
                context.Response.Redirect("/Account/Login");
                return;
            }

            // Session is valid, continue with the request
            await _next(context);
        }

        /// <summary>
        /// Checks if the request path is for a public page that doesn't need auth.
        /// </summary>
        private static bool IsPublicPath(string path)
        {
            foreach (var publicPath in PublicPaths)
            {
                if (path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Root path and empty path are also public
            return string.IsNullOrEmpty(path) || path == "/";
        }

        /// <summary>
        /// Checks if the request is for a static file like CSS, JS, or images.
        /// We don't need to validate sessions for these.
        /// </summary>
        private static bool IsStaticFile(string path)
        {
            // Static files are served from /lib, /css, /js, /uploads, etc.
            var staticPaths = new[] { "/lib/", "/css/", "/js/", "/uploads/", "/favicon" };

            foreach (var staticPath in staticPaths)
            {
                if (path.StartsWith(staticPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Also check for common static file extensions
            var staticExtensions = new[] { ".css", ".js", ".jpg", ".jpeg", ".png", ".gif", ".ico", ".woff", ".woff2", ".ttf" };

            foreach (var ext in staticExtensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Extension method to make it easy to add the middleware in Program.cs
    /// </summary>
    public static class SessionValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionValidationMiddleware>();
        }
    }
}
