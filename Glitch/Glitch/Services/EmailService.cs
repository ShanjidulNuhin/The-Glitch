using System.Net;
using System.Net.Mail;

namespace Glitch.Services
{
    public interface IEmailService
    {
        Task SendPurchaseCongratulationAsync(string customerEmail, string customerName, string adminEmail, string adminName, string gameTitle, decimal gamePrice);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendPurchaseCongratulationAsync(string customerEmail, string customerName, string adminEmail, string adminName, string gameTitle, decimal gamePrice)
        {
            try
            {
                var emailSettings = _config.GetSection("EmailSettings");
                var smtpServer = emailSettings["SmtpServer"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
                var senderEmail = emailSettings["SenderEmail"];
                var senderName = emailSettings["SenderName"];
                var senderPassword = emailSettings["SenderPassword"];

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword) || senderPassword == "your-app-password")
                {
                    // If not configured, just log or return to prevent crashing the purchase flow
                    Console.WriteLine("EmailSettings not configured. Skipping email sending.");
                    return;
                }

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                var fromAddress = new MailAddress(senderEmail, senderName);

                // 1. Send to Customer
                var customerMessage = new MailMessage
                {
                    From = fromAddress,
                    Subject = $"Congratulations on your purchase of {gameTitle}!",
                    Body = $"Hi {customerName},\n\nCongratulations! You have successfully purchased {gameTitle} for ${gamePrice}. You can now download and enjoy your new game.\n\nThank you for choosing The Glitch!",
                    IsBodyHtml = false
                };
                customerMessage.To.Add(customerEmail);
                await client.SendMailAsync(customerMessage);

                // 2. Send to Admin
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    var adminMessage = new MailMessage
                    {
                        From = fromAddress,
                        Subject = $"New Game Sold: {gameTitle}",
                        Body = $"Hi {adminName},\n\nGreat news! {customerName} just purchased {gameTitle} for ${gamePrice}. The amount has been added to your balance.\n\nBest regards,\nThe Glitch System",
                        IsBodyHtml = false
                    };
                    adminMessage.To.Add(adminEmail);
                    await client.SendMailAsync(adminMessage);
                }
            }
            catch (Exception ex)
            {
                // We catch exceptions so that email failures do not stop the purchase process.
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }
}
