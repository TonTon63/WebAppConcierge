namespace WebAppConcierge.Models
{
    /// Modelo utilizado para mostrar información sobre errores en la aplicación.
    public class ErrorViewModel
    {
        // Identificador único de la solicitud HTTP en la que ocurrió el error.
        // Puede ser útil para rastrear errores específicos en los logs del servidor.
        public string? RequestId { get; set; }

        // Devuelve true si existe un RequestId.
        // Se utiliza para decidir si mostrar o no el ID de error en la vista.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
