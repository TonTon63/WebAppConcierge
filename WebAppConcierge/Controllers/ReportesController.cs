using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using WebAppConcierge.Models;

namespace WebAppConcierge.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReportesController : Controller
    {
        private readonly string _cs;

        //Obtiene la cadena de conexion DefaultConnection desde el archivo appsettings.json
        public ReportesController(IConfiguration config)
        {
            _cs = config.GetConnectionString("DefaultConnection");
        }

        //Vista principal del modulo de reportes, con accesos a las demas funcionalidades
        public IActionResult Index()
        {
            return View();
        }

        //Devuelven una vista con la lista de usuarios activos segun el valor booleano. Utiliza el metodo auxiliar para filtrar
        // y retorna a la vista usuariosActivos
        public IActionResult UsuariosActivos()
        {
            var usuarios = ObtenerUsuarios(true);
            return View("UsuariosActivos", usuarios);
        }
        //Generan un archivo .xlsx con los usuarios activos
        public IActionResult ExportarUsuariosActivos() => ExportarUsuarios(true, "Usuarios Activos", "UsuariosActivos.xlsx");

        //Devuelven una vista con la lista de usuarios inactivos segun el valor booleano. Utiliza el metodo auxiliar para filtrar
        // y retorna a la vista usuarios inactivos
        public IActionResult UsuariosInactivos()
        {
            var usuarios = ObtenerUsuarios(false);
            return View("UsuariosInactivos", usuarios);
        }
        //Generan un archivo .xlsx con los usuarios inactivos
        public IActionResult ExportarUsuariosInactivos() => ExportarUsuarios(false, "Usuarios Inactivos", "UsuariosInactivos.xlsx");

        // Muestra todos los usuarios activos agrupados por su rol, se usa un Inner Join entre usuarios, roles y la tabla intermedia
        // y retorna a la vista UsuariosPorRol.cshtml
        public IActionResult UsuariosPorRol()
        {
            var usuarios = new List<Usuario>();

            using var conn = new SqlConnection(_cs);
            using var cmd = new SqlCommand(@"
                SELECT U.TN_Id_Usuario, U.TC_Nombre, U.TC_Apellido, U.TC_Correo_Electronico, R.TC_Nombre_Rol
                FROM TCCR_A_Usuario U
                INNER JOIN TCCR_A_Usuario_Rol UR ON U.TN_Id_Usuario = UR.TN_Id_Usuario
                INNER JOIN TCCR_A_Rol R ON UR.TN_Id_Rol = R.TN_Id_Rol
                WHERE U.TB_Activo = 1
                ORDER BY R.TC_Nombre_Rol", conn);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                usuarios.Add(new Usuario
                {
                    TN_Id_Usuario = reader.GetInt32(0),
                    TC_Nombre = reader.GetString(1),
                    TC_Apellido = reader.GetString(2),
                    TC_Correo_Electronico = reader.GetString(3),
                    NombreRol = reader.GetString(4)
                });
            }

            return View("UsuariosPorRol", usuarios);
        }

        // Devuelve la cantidad total de usuarios por rol, se muestra como una lista de tuplas en la vista cantidadUsuariosPorRol.cshtml
        public IActionResult CantidadUsuariosPorRol()
        {
            var datos = new List<(string Rol, int Cantidad)>();

            using var conn = new SqlConnection(_cs);
            using var cmd = new SqlCommand(@"
                SELECT R.TC_Nombre_Rol, COUNT(*) AS Cantidad
                FROM TCCR_A_Usuario_Rol UR
                INNER JOIN TCCR_A_Rol R ON UR.TN_Id_Rol = R.TN_Id_Rol
                GROUP BY R.TC_Nombre_Rol", conn);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                datos.Add((reader.GetString(0), reader.GetInt32(1)));
            }

            return View("CantidadUsuariosPorRol", datos);
        }



        // Realiza una consulta para obtener todos los usuarios segun su estado de actividad, se usa tanto para vistas como para exportar datos.
        private List<Usuario> ObtenerUsuarios(bool activos)
        {
            var usuarios = new List<Usuario>();

            using var conn = new SqlConnection(_cs);
            using var cmd = new SqlCommand(@"
                SELECT U.TN_Id_Usuario, U.TC_Nombre, U.TC_Apellido, U.TC_Correo_Electronico, 
                       R.TC_Nombre_Rol, U.TB_Activo
                FROM TCCR_A_Usuario U
                INNER JOIN TCCR_A_Usuario_Rol UR ON U.TN_Id_Usuario = UR.TN_Id_Usuario
                INNER JOIN TCCR_A_Rol R ON UR.TN_Id_Rol = R.TN_Id_Rol
                WHERE U.TB_Activo = @Activo", conn);

            cmd.Parameters.AddWithValue("@Activo", activos);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                usuarios.Add(new Usuario
                {
                    TN_Id_Usuario = reader.GetInt32(0),
                    TC_Nombre = reader.GetString(1),
                    TC_Apellido = reader.GetString(2),
                    TC_Correo_Electronico = reader.GetString(3),
                    NombreRol = reader.GetString(4),
                    TB_Activo = reader.GetBoolean(5)
                });
            }

            return usuarios;
        }
        // Crea un DataTable desde una consulta SQL y lo exporta como EXCEl, usa ClosedXML para crear el archivo .xlsx
        // Devulve el archivo al navegador con FILE
        private IActionResult ExportarUsuarios(bool activos, string nombreHoja, string nombreArchivo)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(_cs);
            using var cmd = new SqlCommand(@"
                SELECT U.TN_Id_Usuario AS ID, U.TC_Nombre AS Nombre, 
                       U.TC_Apellido AS Apellido, U.TC_Correo_Electronico AS Correo,
                       R.TC_Nombre_Rol AS Rol
                FROM TCCR_A_Usuario U
                INNER JOIN TCCR_A_Usuario_Rol UR ON U.TN_Id_Usuario = UR.TN_Id_Usuario
                INNER JOIN TCCR_A_Rol R ON UR.TN_Id_Rol = R.TN_Id_Rol
                WHERE U.TB_Activo = @Activo", conn);

            cmd.Parameters.AddWithValue("@Activo", activos);

            conn.Open();
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            using var wb = new XLWorkbook();
            wb.Worksheets.Add(dt, nombreHoja);

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreArchivo);
        }


    }
}


