using System.Net;
using System.Net.Mail;

namespace Tontine.Services
{
    public interface IEmailService
    {
        Task EnvoyerEmailAsync(string destinataire, string sujet, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _conf;

        public EmailService(IConfiguration conf)
        {
            _conf = conf;
        }

        public async Task EnvoyerEmailAsync(string destinataire, string sujet, string message)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("randriamparanyeric@gmail.com", "pjbt xqhh bzsr zfdx"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("randriamparanyeric@gmail.com", "Tontine Gestion"),
                Subject = sujet,
                Body = message,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(destinataire);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}