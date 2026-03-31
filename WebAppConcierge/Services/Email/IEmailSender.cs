using System.Threading.Tasks;

namespace WebAppConcierge.Services.Email
{
    // Contrato para un servicio de envío de correos electrónicos.
    // Implementado por clases que encapsulan la lógica de envío de mensajes
    // (por ejemplo, a través de SMTP, servicios en la nube, colas de fondo, etc.).
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string htmlBody);
    }
}