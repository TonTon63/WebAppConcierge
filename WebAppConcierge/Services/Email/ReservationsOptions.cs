namespace WebAppConcierge.Services.Email
{
    // Clase que representa las opciones de configuración relacionadas con reservas.
    // Se utiliza principalmente para definir parámetros globales como el correo del administrador.
    public class ReservationsOptions
    {
        /// Dirección de correo electrónico del administrador que debe recibir notificaciones sobre reservas.
        /// Este campo puede ser opcional, pero si se especifica, se utilizará para notificar eventos como creación,
        /// actualización o cancelación de reservas por parte de los usuarios.
        public string AdminEmail { get; set; } = "";
    }
}