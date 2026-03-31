using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace WebAppConcierge.Services.Email
{
    /// Implementación de IEmailSender que utiliza SMTP para enviar correos electrónicos.
    /// Utiliza la configuración definida en SmtpOptions (host, puerto, usuario, contraseña, etc.).
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _opt;

        /// Constructor que inyecta las opciones de configuración SMTP desde appsettings.json.
        public SmtpEmailSender(IOptions<SmtpOptions> opt)
        {
            _opt = opt.Value;
        }
        /// Envía un correo electrónico en formato HTML utilizando SMTP.
        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            using var msg = new MailMessage();
            msg.From = new MailAddress(_opt.User, _opt.FromName);
            msg.To.Add(to);
            msg.Subject = subject;
            msg.Body = htmlBody;
            msg.IsBodyHtml = true;

            using var smtp = new SmtpClient(_opt.Host, _opt.Port)
            {
                EnableSsl = _opt.EnableSsl,
                Credentials = new NetworkCredential(_opt.User, _opt.Password)
            };

            await smtp.SendMailAsync(msg);
        }
    }
}
