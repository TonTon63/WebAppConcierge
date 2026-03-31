using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using WebAppConcierge.Models;

namespace WebAppConcierge.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly string _cs;

        // Se obtiene la cadena de conexion desde el archivo de configuracion
        public UsuariosController(IConfiguration config)
        {
            _cs = config.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("Falta la cadena 'DefaultConnection'.");
        }

        // Metodo auxiliar para cargar los roles permitidos al ViewBag (filtrados por el rol actual)
        private void LoadRolesIntoViewBag()
        {
            var currentRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            var roles = new List<(int Id, string Name)>();

            using var conn = new SqlConnection(_cs);
            using var cmd = new SqlCommand("usp_GetRolesForUser", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@CurrentRole", currentRole);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                roles.Add((
                    reader.GetInt32(reader.GetOrdinal("TN_Id_Rol")),
                    reader.GetString(reader.GetOrdinal("TC_Nombre_Rol"))
                ));
            }

            ViewBag.Roles = roles;
        }

        // Vista principal del listado de usuarios (solo para administradores)
        [Authorize(Roles = "Administrador")]
        public IActionResult Index()
        {
            var usuarios = new List<Usuario>();

            using var conn = new SqlConnection(_cs);
            using var cmd = new SqlCommand("usp_GetUsuariosConRol", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

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

            return View(usuarios);
        }

        // Mostrar detalles de un usuario por ID
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Details(int id)
        {
            Usuario usuario = null;

            using var conn = new SqlConnection(_cs);
            using var cmd = new SqlCommand(@"
                SELECT U.TN_Id_Usuario, U.TC_Nombre, U.TC_Apellido, U.TC_Correo_Electronico, 
                       U.TC_Cedula, U.TC_Telefono, U.TB_Activo, R.TC_Nombre_Rol
                FROM TCCR_A_Usuario U
                INNER JOIN TCCR_A_Usuario_Rol UR ON U.TN_Id_Usuario = UR.TN_Id_Usuario
                INNER JOIN TCCR_A_Rol R ON UR.TN_Id_Rol = R.TN_Id_Rol
                WHERE U.TN_Id_Usuario = @Id", conn);

            cmd.Parameters.AddWithValue("@Id", id);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                usuario = new Usuario
                {
                    TN_Id_Usuario = reader.GetInt32(0),
                    TC_Nombre = reader.GetString(1),
                    TC_Apellido = reader.GetString(2),
                    TC_Correo_Electronico = reader.GetString(3),
                    TC_Cedula = reader.GetString(4),
                    TC_Telefono = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    TB_Activo = reader.GetBoolean(6),
                    TC_Nombre_Rol = reader.GetString(7)
                };
            }

            if (usuario == null)
                return NotFound();

            return View(usuario);
        }

        // Vista de edición (se reutiliza la vista de detalles)
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Edit(int id)
        {
            return Details(id); 
        }

        // POST para guardar los cambios de un usuario
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public IActionResult Edit(Usuario model)
        {
            using var conn = new SqlConnection(_cs);
            using var cmd = new SqlCommand(@"
                UPDATE TCCR_A_Usuario 
                SET TC_Nombre = @Nombre, TC_Apellido = @Apellido, 
                    TC_Telefono = @Telefono 
                WHERE TN_Id_Usuario = @Id", conn);

            cmd.Parameters.AddWithValue("@Id", model.TN_Id_Usuario);
            cmd.Parameters.AddWithValue("@Nombre", model.TC_Nombre);
            cmd.Parameters.AddWithValue("@Apellido", model.TC_Apellido);
            cmd.Parameters.AddWithValue("@Telefono", (object?)model.TC_Telefono ?? DBNull.Value);

            conn.Open();
            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
            return RedirectToAction("Index");
        }

        // Acción para desactivar (eliminar) un usuario
        [Authorize(Roles = "Administrador")]
        public IActionResult Delete(int id)
        {
            using var conn = new SqlConnection(_cs);
            conn.Open();

            using var cmd = new SqlCommand(@"
        DELETE FROM TCCR_A_Usuario
        WHERE TN_Id_Usuario = @Id", conn);

            cmd.Parameters.AddWithValue("@Id", id);

            try
            {
                int filasAfectadas = cmd.ExecuteNonQuery();

                if (filasAfectadas > 0)
                    TempData["SuccessMessage"] = "Usuario eliminado de forma permanente.";
                else
                    TempData["ErrorMessage"] = "No se encontró el usuario para eliminar.";
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = "Error al eliminar el usuario: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
        // Formulario para crear un nuevo usuario
        [HttpGet]
        public IActionResult Create()
        {
            LoadRolesIntoViewBag();
            return View();
        }

        // POST: crear usuario con un rol
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Usuario model, int selectedRoleId)
        {
            LoadRolesIntoViewBag();

            if (selectedRoleId == 0)
                selectedRoleId = 2; // Por defecto asigna rol básico

            try
            {
                using var conn = new SqlConnection(_cs);
                using var cmd = new SqlCommand("usp_RegisterUsuario", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                // Parámetros para el SP de registro
                cmd.Parameters.AddWithValue("@Email", model.TC_Correo_Electronico);
                cmd.Parameters.AddWithValue("@Password", model.TC_Contrasena);
                cmd.Parameters.AddWithValue("@UserName", model.TC_Nombre);
                cmd.Parameters.AddWithValue("@LastName", model.TC_Apellido);
                cmd.Parameters.AddWithValue("@Cedula", model.TC_Cedula);
                cmd.Parameters.AddWithValue("@Telefono", (object?)model.TC_Telefono ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Id_Role", selectedRoleId);

                conn.Open();
                cmd.ExecuteNonQuery();

                TempData["SuccessMessage"] = "Usuario registrado con éxito.";
                return RedirectToAction(nameof(Create));
            }
            catch (SqlException ex) when (ex.Number == 51000 || ex.Number == 51001)
            {
                // Mensajes personalizados desde SQL Server (RAISERROR)
                TempData["ErrorMessage"] = ex.Message;
                return View(model);
            }
            catch
            {
                TempData["ErrorMessage"] = "Ocurrió un error al crear el usuario.";
                return View(model);
            }
        }
    }
}

