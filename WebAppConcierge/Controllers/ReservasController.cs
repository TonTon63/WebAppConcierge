using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using WebAppConcierge.Models;

namespace WebAppConcierge.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReservasController : Controller
    {
        private readonly string _cs;

        public ReservasController(IConfiguration config)
        {
            _cs = config.GetConnectionString("DefaultConnection");
        }

        // Muestra un listado paginado de las reservas, permite aplicar un filtro por nombre de usuario o correo electronico
        // Calcula la cantidad total de registros para determinar el numero de paginas
        public IActionResult Index(string filtro, int pagina = 1, int tamañoPagina = 10)
        {
            var reservas = new List<Reserva>();
            int totalRegistros = 0;

            using var conn = new SqlConnection(_cs);
            conn.Open();

            // Esta consulta SQL con COUNT para obtener la cantidad total de resultados
            var countCmd = new SqlCommand(@"
        SELECT COUNT(*) 
        FROM Reservas R
        INNER JOIN TCCR_A_Usuario U ON R.UsuarioId = U.TN_Id_Usuario
        WHERE (@filtro IS NULL OR U.TC_Nombre LIKE '%' + @filtro + '%' OR U.TC_Correo_Electronico LIKE '%' + @filtro + '%')", conn);
            countCmd.Parameters.AddWithValue("@filtro", (object?)filtro ?? DBNull.Value);
            totalRegistros = (int)countCmd.ExecuteScalar();

           // Aqui se realiza una consulta SQL paginada para obtener una lista de reservas desde la base de datos,
           // aplicando un filtro por nombre o crreo si fue ingresado y limintando los resultados segun la pagina actual.
            var cmd = new SqlCommand(@"
        SELECT R.Id, R.UsuarioId, 
               U.TC_Nombre + ' ' + U.TC_Apellido AS NombreUsuario,
               U.TC_Correo_Electronico, R.Fecha, R.Hora, R.Servicios, 
               R.NotificarInterno, R.FechaCreacion
        FROM Reservas R
        INNER JOIN TCCR_A_Usuario U ON R.UsuarioId = U.TN_Id_Usuario
        WHERE (@filtro IS NULL OR U.TC_Nombre LIKE '%' + @filtro + '%' OR U.TC_Correo_Electronico LIKE '%' + @filtro + '%')
        ORDER BY R.Fecha DESC, R.Hora DESC
        OFFSET @offset ROWS FETCH NEXT @limite ROWS ONLY", conn);

            cmd.Parameters.AddWithValue("@filtro", (object?)filtro ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@offset", (pagina - 1) * tamañoPagina);
            cmd.Parameters.AddWithValue("@limite", tamañoPagina);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                reservas.Add(new Reserva
                {
                    Id = reader.GetInt32(0),
                    UsuarioId = reader.GetInt32(1),
                    NombreUsuario = reader.GetString(2),
                    Correo = reader.GetString(3),
                    Fecha = reader.GetDateTime(4),
                    Hora = reader.GetTimeSpan(5),
                    Servicios = reader.GetString(6),
                    NotificarInterno = reader.GetBoolean(7),
                    FechaCreacion = reader.GetDateTime(8)
                });
            }

            // Enviar datos a la vista
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamañoPagina);
            ViewBag.Filtro = filtro;

            return View(reservas);
        }
        //Elimina una reserva especifica por su ID
        //Si no se elimina correctamente se muestra un mensaje de error
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public IActionResult Eliminar(int id)
        {
            using var conn = new SqlConnection(_cs);
            using var cmd = new SqlCommand("DELETE FROM Reservas WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            conn.Open();
            int rows = cmd.ExecuteNonQuery();

            if (rows == 0)
            {
                TempData["ErrorMessage"] = "No se pudo eliminar la reserva.";
            }
            else
            {
                TempData["SuccessMessage"] = "Reserva eliminada correctamente.";
            }

            return RedirectToAction("Index");
        }

    }
}





