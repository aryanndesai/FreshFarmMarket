using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FreshFarmMarket.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var subject = "Reset Your Password - Fresh Farm Market";
            var htmlBody =
                $@"
                <html>
                <body style='font-family: Arial, sans-serif; padding: 20px; background-color: #f4f4f4;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <h2 style='color: #28a745; margin-bottom: 20px;'>
                            <span style='font-size: 30px;'>🔐</span> Password Reset Request
                        </h2>
                        <p style='font-size: 16px; color: #333; line-height: 1.6;'>Hi there,</p>
                        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
                            You requested to reset your password for your Fresh Farm Market account.
                        </p>
                        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
                            Click the button below to reset your password:
                        </p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' 
                               style='background-color: #28a745; 
                                      color: white; 
                                      padding: 15px 40px; 
                                      text-decoration: none; 
                                      border-radius: 5px; 
                                      font-size: 16px; 
                                      font-weight: bold;
                                      display: inline-block;'>
                                Reset Password
                            </a>
                        </div>
                        <p style='font-size: 14px; color: #666; line-height: 1.6;'>
                            Or copy and paste this link into your browser:
                        </p>
                        <p style='font-size: 14px; color: #0066cc; word-break: break-all;'>
                            {resetLink}
                        </p>
                        <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;'>
                            <p style='margin: 0; color: #856404; font-size: 14px;'>
                                <strong>⚠️ Security Notice:</strong> This link will expire in 1 hour.
                            </p>
                        </div>
                        <p style='font-size: 14px; color: #666; line-height: 1.6;'>
                            If you didn't request this password reset, please ignore this email. 
                            Your password will remain unchanged.
                        </p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                        <p style='font-size: 14px; color: #999; text-align: center;'>
                            Best regards,<br>
                            <strong>Fresh Farm Market Team</strong>
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, htmlBody);
        }

        public async Task SendTwoFactorCodeAsync(string email, string code)
        {
            var subject = "Your Verification Code - Fresh Farm Market";
            var htmlBody =
                $@"
                <html>
                <body style='font-family: Arial, sans-serif; padding: 20px; background-color: #f4f4f4;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <h2 style='color: #28a745; margin-bottom: 20px;'>
                            <span style='font-size: 30px;'>🔒</span> Two-Factor Authentication
                        </h2>
                        <p style='font-size: 16px; color: #333; line-height: 1.6;'>Hi there,</p>
                        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
                            You're logging into your Fresh Farm Market account. 
                            Please use the verification code below:
                        </p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <div style='background-color: #f8f9fa; 
                                        padding: 20px; 
                                        border-radius: 10px; 
                                        border: 2px solid #28a745;
                                        display: inline-block;'>
                                <h1 style='margin: 0; 
                                           color: #28a745; 
                                           font-size: 36px; 
                                           letter-spacing: 10px; 
                                           font-family: monospace;'>
                                    {code}
                                </h1>
                            </div>
                        </div>
                        <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;'>
                            <p style='margin: 0; color: #856404; font-size: 14px;'>
                                <strong>⏰ Important:</strong> This code will expire in 5 minutes.
                            </p>
                        </div>
                        <p style='font-size: 14px; color: #666; line-height: 1.6;'>
                            If you didn't attempt to log in, please ignore this email and 
                            consider changing your password.
                        </p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                        <p style='font-size: 14px; color: #999; text-align: center;'>
                            Best regards,<br>
                            <strong>Fresh Farm Market Team</strong>
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, htmlBody);
        }

        public async Task SendWelcomeEmailAsync(string email, string fullName)
        {
            var subject = "Welcome to Fresh Farm Market! 🌱";
            var htmlBody =
                $@"
                <html>
                <body style='font-family: Arial, sans-serif; padding: 20px; background-color: #f4f4f4;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <h2 style='color: #28a745; margin-bottom: 20px;'>
                            <span style='font-size: 30px;'>🌱</span> Welcome to Fresh Farm Market!
                        </h2>
                        <p style='font-size: 16px; color: #333; line-height: 1.6;'>Hi <strong>{fullName}</strong>,</p>
                        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
                            Thank you for joining Fresh Farm Market! Your account has been created successfully.
                        </p>
                        <div style='background-color: #d4edda; 
                                    border-left: 4px solid #28a745; 
                                    padding: 15px; 
                                    margin: 20px 0;'>
                            <p style='margin: 0; color: #155724; font-size: 16px;'>
                                <strong>✅ Your account is now active!</strong>
                            </p>
                        </div>
                        <p style='font-size: 16px; color: #333; line-height: 1.6;'>
                            You can now log in and start shopping for fresh, organic produce delivered 
                            straight to your door.
                        </p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='https://localhost:7173/Account/Login' 
                               style='background-color: #28a745; 
                                      color: white; 
                                      padding: 15px 40px; 
                                      text-decoration: none; 
                                      border-radius: 5px; 
                                      font-size: 16px; 
                                      font-weight: bold;
                                      display: inline-block;'>
                                Login Now
                            </a>
                        </div>
                        <p style='font-size: 14px; color: #666; line-height: 1.6;'>
                            <strong>Security Note:</strong> Your password is encrypted and secure. 
                            We've enabled two-factor authentication to keep your account safe.
                        </p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                        <p style='font-size: 14px; color: #999; text-align: center;'>
                            Best regards,<br>
                            <strong>Fresh Farm Market Team</strong>
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, htmlBody);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var apiKey = _configuration["Email:SendGridApiKey"];
                var fromEmail = _configuration["Email:FromEmail"];
                var fromName = _configuration["Email:FromName"];

                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(fromEmail, fromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlBody);

                var response = await client.SendEmailAsync(msg);

                // Log the response for debugging
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Email sent successfully to {toEmail}");
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    Console.WriteLine($"❌ SendGrid error: {response.StatusCode} - {responseBody}");
                    throw new Exception($"SendGrid error: {response.StatusCode} - {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email sending failed: {ex.Message}");
                throw;
            }
        }
    }
}
