using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace EVO_Backend.Services
{
    public class MailOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public string User { get; set; } = "";
        public string Pass { get; set; } = "";
        public string FromName { get; set; } = "EVO Team";
        public string FromEmail { get; set; } = "no-reply@evoteam.local";
    }

    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string bodyHtml);
    }

    public class EmailService : IEmailService
    {
        private readonly MailOptions _opt;
        public EmailService(IOptions<MailOptions> opt) => _opt = opt.Value;

        public async Task SendAsync(string to, string subject, string bodyHtml)
        {
            using var client = new SmtpClient
            {
                Host = _opt.Host,
                Port = _opt.Port,
                EnableSsl = true, // ضروري لميل تريب
                Credentials = new NetworkCredential(_opt.User, _opt.Pass),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            var message = new MailMessage
            {
                From = new MailAddress(_opt.FromEmail, _opt.FromName),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true
            };

            message.To.Add(to);
            await client.SendMailAsync(message);
        }
    }
}
