//Estas dependencias permiten trabajar con formularios web, autenticación por cookies,
//acceso a bases de datos SQL Server y gestión de identidades.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using WebAppConcierge.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebAppConcierge.Controllers
{
    public class AuthController : Controller
    {
        
        private readonly string _cs; // Esta es la cadena de conexion a SQL Server tomada desde el appsettings.json
        private readonly PasswordHasher<string> _hasher = new();

        public AuthController(IConfiguration config)
        {
            _cs = config.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("Falta la cadena 'DefaultConnection' en configuración.");
        }

        [HttpGet]
        
        public IActionResult Login() => View(); //Muestra la vista de inicio de sesion 

        //Procesa el formulario de login, utiliza el stored procedure usp_LoginUsuario para obtener datos
        //del usuario segun el correo electronico, valida la contrasena con la que esta guardada en la base de datos
        // si es valida crea un conjunto de Claims (Id, nombre, rol), inicia sesion usando cookies y redirige al Home/Index
        // Si no es valida muestra errores apropiados en la vista. 
        [HttpPost]
        public IActionResult Login(string email, string password) 
        {
            int userId;
            string storedPassword, userName, role;

            using (var conn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand("usp_LoginUsuario", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 256) { Value = email });

                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    ModelState.AddModelError("", "Comunícate con Soporte.");
                    return View();
                }

                userId = reader.GetInt32(reader.GetOrdinal("TN_Id_Usuario"));
                storedPassword = reader.GetString(reader.GetOrdinal("TC_Contrasena"));
                userName = reader.GetString(reader.GetOrdinal("TC_Nombre"));
                role = reader.GetString(reader.GetOrdinal("TC_Nombre_Rol"));
            }

            if (storedPassword != password)
            {
                ModelState.AddModelError("", "Email o contraseña inválida.");
                return View();
            }

                var claims = new[]
                {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name,            userName),
                new Claim(ClaimTypes.Role,            role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            ).Wait();

            TempData["SuccessMessage"] = "Identificado Correctamente.";
          
            return RedirectToAction("Index", "Home");
        }

        //Aca se cierra la sesion actual del usuario y redigire nuevamnete a la pagina de login
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            return RedirectToAction("Login");
        }

        //Se muestra una vista de aceeso denegado si el usuario intenta acceder a una pagina sin los permisos necesarios
        public IActionResult AccessDenied() => View();
    }
}
