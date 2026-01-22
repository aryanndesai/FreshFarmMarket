using System;
using System.Threading.Tasks;

namespace FreshFarmMarket.Services
{
    public class EmailService
    {
        // In production, use SendGrid, AWS SES, or SMTP
        // For now, we'll simulate email sending and log to console
        
        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            // Simulate async operation
            await Task.Delay(100);

            // In production, send actual email here
            Console.WriteLine($"=== PASSWORD RESET EMAIL ===");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Subject: Reset Your Password");
            Console.WriteLine($"Body: Click this link to reset your password: {resetLink}");
            Console.WriteLine($"Link expires in 1 hour.");
            Console.WriteLine($"============================");

            // TODO: Implement real email sending
            // Example with SMTP:
            // using var client = new SmtpClient("smtp.gmail.com", 587);
            // client.Credentials = new NetworkCredential("your-email", "your-password");
            // client.EnableSsl = true;
            // await client.SendMailAsync(new MailMessage(from, to, subject, body));
        }

        public async Task SendTwoFactorCodeAsync(string email, string code)
        {
            // Simulate async operation
            await Task.Delay(100);

            // In production, send actual email here
            Console.WriteLine($"=== TWO-FACTOR CODE EMAIL ===");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Subject: Your Verification Code");
            Console.WriteLine($"Body: Your verification code is: {code}");
            Console.WriteLine($"Code expires in 5 minutes.");
            Console.WriteLine($"=============================");

            // TODO: Implement real email sending
        }

        public async Task SendWelcomeEmailAsync(string email, string fullName)
        {
            await Task.Delay(100);

            Console.WriteLine($"=== WELCOME EMAIL ===");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Subject: Welcome to Fresh Farm Market!");
            Console.WriteLine($"Body: Hi {fullName}, welcome to Fresh Farm Market!");
            Console.WriteLine($"=====================");
        }
    }
}