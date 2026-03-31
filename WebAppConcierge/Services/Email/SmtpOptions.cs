namespace WebAppConcierge.Services.Email
{
    /// Clase que encapsula las opciones de configuración necesarias para el envío de correos SMTP.
    /// Utilizada por la clase <see cref="SmtpEmailSender"/> y configurada normalmente desde appsettings.json.
    public class SmtpOptions
    {
        /// Dirección del servidor SMTP (por ejemplo, smtp.gmail.com).
        public string Host { get; set; } = "";
        /// Puerto a utilizar para el servidor SMTP (por defecto 587 para STARTTLS).
        public int Port { get; set; } = 587;
        /// Indica si se debe usar SSL/TLS para la conexión SMTP.
        public bool EnableSsl { get; set; } = true;
        /// Correo electrónico del remitente autenticado (usado como usuario SMTP).
        public string User { get; set; } = "";
        /// Contraseña o contraseña de aplicación asociada al correo.
        public string Password { get; set; } = "";
        /// Nombre que se mostrará como remitente en los correos enviados.
        public string FromName { get; set; } = "Concierge Costa Rica";
    }
}