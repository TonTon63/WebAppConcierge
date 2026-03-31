using System.ComponentModel.DataAnnotations;

namespace WebAppConcierge.Models
{
    /// Representa una reserva realizada por un usuario en el sistema Concierge.
    public class Reserva
    {
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public TimeSpan Hora { get; set; }

        [Required]
        public string Servicios { get; set; } // CSV

        // NUEVO: País donde solicita los servicios
        public string? Pais { get; set; }

        public string? NombreUsuario { get; set; }
        public string? Correo { get; set; }
        public bool NotificarInterno { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
