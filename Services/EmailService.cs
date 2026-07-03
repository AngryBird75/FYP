using System.Net;
using System.Net.Mail;

namespace AspiraHub.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var host = _config["Smtp:Host"];
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];

            // No SMTP credentials configured (e.g. local dev) — fall back to
            // logging instead of throwing, so the flow can still be tested.
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("SMTP not configured — email NOT sent. To: {To}, Subject: {Subject}, Body: {Body}",
                    toEmail, subject, body);
                return;
            }

            var port = _config.GetValue<int?>("Smtp:Port") ?? 587;
            var enableSsl = _config.GetValue<bool?>("Smtp:EnableSsl") ?? true;
            var fromEmail = _config["Smtp:FromEmail"] ?? username;
            var fromName = _config["Smtp:FromName"] ?? "Aspira Hub";

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                // Don't let a mail-server hiccup blow up the request — log it
                // so an admin can investigate, but the OTP row already exists
                // in the DB so the user can be told to check their inbox/retry.
                _logger.LogError(ex, "Failed to send email to {To}", toEmail);
            }
        }
    }
}
