using System;
using System.Threading.Tasks;
using FreshFarmMarket.Data;
using FreshFarmMarket.Models;
using Microsoft.AspNetCore.Http;

namespace FreshFarmMarket.Services
{
    public class AuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, int? userId = null, string? details = null, bool success = true)
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Details = details ?? string.Empty,
                IpAddress = ipAddress ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                Success = success
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}