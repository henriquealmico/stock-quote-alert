using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using StockPricesAlert.Configurations;

namespace StockPricesAlert
{
    public class EmailService(Config config, ILogger logger)
    {
        public async Task SendEmailAlertAsync(string stockSymbol, decimal stockPrice, string alertType)
        {
            MimeMessage email = new MimeMessage();
            email.From.Add(new MailboxAddress("Stock Alert", config.EmailSettings.FromEmail));
            email.To.Add(new MailboxAddress("Recipient", config.EmailSettings.ToEmail));
            email.Subject = $"{alertType} Alert for {stockSymbol}";
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = $"<b>{alertType} triggered for {stockSymbol} at price {stockPrice}</b>"
            };

            try
            {
                using SmtpClient smtp = new SmtpClient();
                await smtp.ConnectAsync(config.EmailSettings.SmtpServer, config.EmailSettings.SmtpPort, false);
                await smtp.AuthenticateAsync(config.EmailSettings.FromEmail, config.EmailSettings.EmailPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                logger.LogInformation("Email alert sent successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to send email alert");
            }
        }
    }
}
