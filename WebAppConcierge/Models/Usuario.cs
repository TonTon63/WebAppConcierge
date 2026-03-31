using System.ComponentModel.DataAnnotations;

namespace WebAppConcierge.Models
{
    // Representa un usuario del sistema, ya sea cliente o administrador. 
    public class Usuario
    {
        // Identificador único del usuario en la base de datos. 
        public int TN_Id_Usuario { get; set; }

        // Correo electrónico del usuario. Es obligatorio y debe tener un formato válido.
        [Required, EmailAddress]
        public string TC_Correo_Electronico { get; set; }

        // Contraseña del usuario. Es obligatoria.
        [Required]
        public string TC_Contrasena { get; set; }

        // Nombre del usuario. Es el nombre que se mostrará en las vistas. 
        [Required]
        public string TC_Nombre { get; set; }

        // Apellido del usuario. Es obligatorio.
        [Required]
        public string TC_Apellido { get; set; }

        // Cédula del usuario. Es obligatoria.
        [Required]
        public string TC_Cedula { get; set; }

        // Número de teléfono del usuario. Puede ser nulo. 
        [Required]
        public string? TC_Telefono { get; set; }

        // Rol del usuario (ej. Administrador, Cliente). Se usa para vistas generales. 
        public string NombreRol { get; set; }

        // Indica si el usuario está activo. `true` para activo, `false` para dado de baja.
        public bool TB_Activo { get; set; }  // 1 = activo, 0 = dado de baja

        // Nombre del rol obtenido desde el stored procedure `usp_GetUsuariosConRol`.
        public string TC_Nombre_Rol { get; set; }


    }
}
