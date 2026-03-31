using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;

namespace WebAppConcierge.Controllers
{
    public class ContactoController : Controller
    {
        //Aca se muestra la vista del formulario de contacto (Views/Contacto/Index.cshtml
        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/Home/contactar.cshtml");
        }

        [HttpPost]
        // [ValidateAntiForgeryToken]  // Recomendado: habilitar Anti-CSRF si tu formulario incluye @Html.AntiForgeryToken()
        public IActionResult Index(string Nombre, string Email, string Asunto, string Mensaje)
        {
            // 1) Validación básica de campos requeridos
            if (string.IsNullOrWhiteSpace(Nombre) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Asunto) ||
                string.IsNullOrWhiteSpace(Mensaje))
            {
                // Envía un mensaje de error a la vista para notificar al usuario
                ViewData["ErrorMessage"] = "Por favor complete todos los campos.";
                return View("~/Views/Home/contactar.cshtml");
            }

            try
            {
                // 2) Configuración del remitente y credenciales 
                var remitente = "tonvasroj060603@gmail.com"; //  correo de Gmail
                var contrasenaApp = "xobx mlom munm eayr"; //  contraseña generada por Google

                // 3) Construcción del correo que llega al administrador
                var mailToAdmin = new MailMessage();
                mailToAdmin.From = new MailAddress(remitente, "Formulario Web Concierge");
                mailToAdmin.To.Add(remitente);
                mailToAdmin.Subject = $"[Formulario Contacto] {Asunto}";
                mailToAdmin.Body = $@"
                    <p><strong>Nombre:</strong> {Nombre}</p>
                    <p><strong>Email:</strong> {Email}</p>
                    <p><strong>Mensaje:</strong><br>{Mensaje}</p>";
                mailToAdmin.IsBodyHtml = true;

                // 4) Construcción del correo de confirmación para el cliente
                var mailToCliente = new MailMessage();
                mailToCliente.From = new MailAddress(remitente, "Concierge Costa Rica");
                mailToCliente.To.Add(Email);
                mailToCliente.Subject = "Gracias por contactarnos - Concierge Costa Rica";
                mailToCliente.Body = @"
                    <p>Estimado/a cliente,</p>
                    <p>¡Gracias por contactarnos!</p>
                    <p>Nuestro departamento de operaciones está revisando su solicitud. Nos pondremos en contacto con usted dentro de las próximas 24 horas.</p>
                    <p>Agradecemos su preferencia.</p>
                    <p><strong>Equipo de Operaciones, Costa Rica Concierge</strong></p>";
                mailToCliente.IsBodyHtml = true;

                // 5) Envío SMTP vía Gmail (puerto 587 + STARTTLS)
                using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtpClient.Credentials = new NetworkCredential(remitente, contrasenaApp);
                    smtpClient.EnableSsl = true;

                    // Se envían ambos correos (admin y confirmación al cliente)
                    smtpClient.Send(mailToAdmin);
                    smtpClient.Send(mailToCliente);
                }

                // 6) Feedback en la UI y limpieza del estado del formulario
                ViewData["SuccessMessage"] = "¡Mensaje enviado con éxito! Hemos enviado una confirmación a su correo.";
                ModelState.Clear();
                return View("~/Views/Home/contactar.cshtml");
            }
            catch (System.Exception ex)
            {
                // Si algo falla en el envío, informamos el error
                ViewData["ErrorMessage"] = $"Error al enviar el mensaje: {ex.Message}";
                return View("~/Views/Home/contactar.cshtml");
            }
        }
    }
}
