using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;
using WebAppConcierge.Models;
using WebAppConcierge.Services.Email;

namespace WebAppConcierge.Controllers
{
    //Controlador para gestionar reservas en el sistema Concierge Costa Rica.
    //Requiere autenticación. Permite crear, modificar, cancelar y listar reservas,
    //así como enviar correos automatizados al usuario y al administrador.
    [Authorize]
    public class PruebaController : Controller
    {
        // Cadena de conexión a SQL Server
        private readonly string _cs;
        // Servicio de envío de correos
        private readonly IEmailSender _email;
        // Configuración relacionada a reservas (AdminEmail)
        private readonly ReservationsOptions _resOpt;

        //Constructor del controlador. Inyecta configuración, servicio de correo y opciones de reserva.
        public PruebaController(IConfiguration config, IEmailSender email, IOptions<ReservationsOptions> resOpt)
        {
            _cs = config.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("Falta la cadena 'DefaultConnection' en configuración.");
            _email = email;
            _resOpt = resOpt.Value;
        }

        //Muestra el panel principal del usuario.
        public IActionResult Panel() => View();
        // Mustra la vista par ver o iniciar una nueva reserva
        public IActionResult Reservas() => View();
       
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Crea una nueva reserva, guarda en base de datos y envía correos automáticos al usuario y al administrador (opcional).
        public IActionResult Crear(IFormCollection form)
        {
            try
            {
                int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Leer país
                var pais = form["Pais"].ToString();

                // Convertir acrónimo a nombre completo
                string nombrePais = pais switch
                {
                    "cr" => "Costa Rica",
                    "tc" => "Turks and Caicos",
                    "mx" => "México",
                    "us" => "Estados Unidos",
                    "bs" => "Bahamas",
                    _ => pais // por si no coincide
                };

                var fecha = DateTime.Parse(form["Fecha"]);
                var hora = TimeSpan.Parse(form["Hora"]);
                var notificar = form["NotificarInterno"].FirstOrDefault() == "on";
                var serviciosSeleccionados = form["Servicios"];
                string servicios = string.Join(", ", serviciosSeleccionados.Select(x => x.Trim()).Distinct());

                int nuevaReservaId;

                using (var con = new SqlConnection(_cs))
                {
                    con.Open();
                    using var cmd = new SqlCommand(@"
                INSERT INTO Reservas (UsuarioId, Fecha, Hora, Servicios, NotificarInterno, FechaCreacion, Pais) 
                OUTPUT INSERTED.Id 
                VALUES (@UsuarioId, @Fecha, @Hora, @Servicios, @Notificar, GETDATE(), @Pais)", con);
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@Fecha", fecha.Date);
                    cmd.Parameters.AddWithValue("@Hora", hora);
                    cmd.Parameters.AddWithValue("@Servicios", servicios);
                    cmd.Parameters.AddWithValue("@Notificar", notificar);
                    cmd.Parameters.AddWithValue("@Pais", string.IsNullOrWhiteSpace(pais) ? (object)DBNull.Value : pais);

                    nuevaReservaId = (int)cmd.ExecuteScalar();
                }

                var (nombreUsuario, correoUsuario) = ObtenerNombreYCorreoUsuario(usuarioId);

                EnviarCorreoUsuario_ReservaCreada(nuevaReservaId, nombreUsuario, correoUsuario, fecha, hora, servicios, nombrePais);

                if (notificar && !string.IsNullOrWhiteSpace(_resOpt.AdminEmail))
                    EnviarCorreoAdmin_ReservaCreada(nuevaReservaId, nombreUsuario, correoUsuario, fecha, hora, servicios, nombrePais);

                return RedirectToAction("ConfirmarReserva", new { id = nuevaReservaId });
            }
            catch (Exception ex)
            {
                TempData["ReservaErrorMessage"] = "Error al realizar la reserva: " + ex.Message;
                return RedirectToAction("Reservar");
            }
        }

        // ACTUALIZAR: envía correo a usuario + (opcional) admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Actualizar(IFormCollection form)
        {
            try
            {
                int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                int reservaId = int.Parse(form["Id"]);

                // País
                var pais = form["Pais"].ToString();

                var fecha = DateTime.Parse(form["Fecha"]);
                var hora = TimeSpan.Parse(form["Hora"]);
                var notificar = form["NotificarInterno"].FirstOrDefault() == "on";
                var serviciosSeleccionados = form["Servicios"];
                string servicios = string.Join(", ", serviciosSeleccionados.Select(x => x.Trim()).Distinct());

                using (var con = new SqlConnection(_cs))
                {
                    con.Open();

                    using (var checkCmd = new SqlCommand("SELECT UsuarioId FROM Reservas WHERE Id = @Id", con))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", reservaId);
                        var result = checkCmd.ExecuteScalar();
                        if (result == null || (int)result != usuarioId)
                        {
                            TempData["ErrorMessage"] = "No tienes permiso para modificar esta reserva.";
                            return RedirectToAction("MisReservas");
                        }
                    }

                    using var cmd = new SqlCommand(@"
                UPDATE Reservas 
                SET Fecha = @Fecha, Hora = @Hora, Servicios = @Servicios, NotificarInterno = @NotificarInterno, Pais = @Pais
                WHERE Id = @Id", con);
                    cmd.Parameters.AddWithValue("@Fecha", fecha.Date);
                    cmd.Parameters.AddWithValue("@Hora", hora);
                    cmd.Parameters.AddWithValue("@Servicios", servicios);
                    cmd.Parameters.AddWithValue("@NotificarInterno", notificar);
                    cmd.Parameters.AddWithValue("@Pais", string.IsNullOrWhiteSpace(pais) ? (object)DBNull.Value : pais);
                    cmd.Parameters.AddWithValue("@Id", reservaId);

                    cmd.ExecuteNonQuery();
                }

                var (nombreUsuario, correoUsuario) = ObtenerNombreYCorreoUsuario(usuarioId);

                // Convertir acrónimo a nombre completo del país
                string paisNombreCompleto = ObtenerNombrePais(pais);

                // Enviar correos con el nombre completo del país
                EnviarCorreoUsuario_ReservaActualizada(reservaId, nombreUsuario, correoUsuario, fecha, hora, servicios, paisNombreCompleto);
                EnviarCorreoAdmin_ReservaActualizada(reservaId, nombreUsuario, correoUsuario, fecha, hora, servicios, paisNombreCompleto);

                TempData["ReservaSuccessMessage"] = "Reserva actualizada correctamente.";
                return RedirectToAction("MisReservas");
            }
            catch (Exception ex)
            {
                TempData["ReservaErrorMessage"] = "Error al actualizar la reserva: " + ex.Message;
                return RedirectToAction("MisReservas");
            }
        }

        // Cancela una reserva del usuario. Elimina el registro y envía correos de cancelación.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancelar(int id)
        {
            try
            {
                int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                DateTime? fecha = null;
                TimeSpan? hora = null;
                string servicios = "", pais = "";
                string nombre = "", correo = "";

                using (var con = new SqlConnection(_cs))
                {
                    con.Open();

                    using (var checkCmd = new SqlCommand("SELECT UsuarioId, Fecha, Hora, Servicios, NotificarInterno, Pais FROM Reservas WHERE Id = @Id", con))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        using var rd = checkCmd.ExecuteReader();
                        if (!rd.Read() || (int)rd["UsuarioId"] != usuarioId)
                        {
                            TempData["ReservaErrorMessage"] = "No tienes permiso para cancelar esta reserva.";
                            return RedirectToAction("MisReservas");
                        }

                        fecha = (DateTime)rd["Fecha"];
                        hora = (TimeSpan)rd["Hora"];
                        servicios = rd["Servicios"].ToString();
                        pais = rd["Pais"].ToString();
                    }

                    using var deleteCmd = new SqlCommand("DELETE FROM Reservas WHERE Id = @Id", con);
                    deleteCmd.Parameters.AddWithValue("@Id", id);
                    deleteCmd.ExecuteNonQuery();
                }

                // Obtener datos del usuario
                var datos = ObtenerNombreYCorreoUsuario(usuarioId);
                nombre = datos.Nombre;
                correo = datos.Correo;

                // Convertir acrónimo a nombre completo del país
                string paisNombre = ObtenerNombrePais(pais);

                // Enviar correo al usuario
                EnviarCorreoUsuario_ReservaCancelada(id, nombre, correo, fecha.Value, hora.Value, servicios, paisNombre);

                // Enviar correo al admin SIEMPRE
                if (!string.IsNullOrWhiteSpace(_resOpt.AdminEmail))
                {
                    try
                    {
                        EnviarCorreoAdmin_ReservaCancelada(id, nombre, correo, fecha.Value, hora.Value, servicios, paisNombre);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error al enviar correo al admin: " + ex.Message);
                    }
                }

                TempData["ReservaSuccessMessage"] = "Reserva cancelada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["ReservaErrorMessage"] = "Error al cancelar la reserva: " + ex.Message;
            }

            return RedirectToAction("MisReservas");
        }

        // Muestra el formulario para modificar una reserva existente del usuario.
        public IActionResult Modificar(int id)
        {
            Reserva reserva = null;

            using (var con = new SqlConnection(_cs))
            {
                con.Open();
                using var cmd = new SqlCommand("SELECT * FROM Reservas WHERE Id = @Id", con);
                cmd.Parameters.AddWithValue("@Id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    reserva = new Reserva
                    {
                        Id = (int)reader["Id"],
                        UsuarioId = (int)reader["UsuarioId"],
                        Fecha = (DateTime)reader["Fecha"],
                        Hora = (TimeSpan)reader["Hora"],
                        Servicios = reader["Servicios"].ToString(),
                        NotificarInterno = (bool)reader["NotificarInterno"]
                    };
                }
            }

            if (reserva == null)
            {
                TempData["ErrorMessage"] = "Reserva no encontrada.";
                return RedirectToAction("Reservar");
            }

            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (reserva.UsuarioId != usuarioId)
            {
                TempData["ErrorMessage"] = "No tienes permiso para modificar esta reserva.";
                return RedirectToAction("Reservar");
            }

            return View(reserva);
        }

        // Muestra la vista de formulario de reserva con la última reserva hecha por el usuario.
        public IActionResult Reservar()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            int? ultimaReservaId = null;

            using (var con = new SqlConnection(_cs))
            {
                con.Open();
                using var cmd = new SqlCommand(
                    "SELECT TOP 1 Id FROM Reservas WHERE UsuarioId = @UsuarioId ORDER BY FechaCreacion DESC", con);
                cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                var result = cmd.ExecuteScalar();
                if (result != null) ultimaReservaId = Convert.ToInt32(result);
            }

            ViewBag.ReservaId = ultimaReservaId;
            return View();
        }

        // Muestra los detalles de confirmación después de una reserva exitosa.
        public IActionResult ConfirmarReserva(int id)
        {
            Reserva reserva = null;

            using (var con = new SqlConnection(_cs))
            {
                con.Open();
                using var cmd = new SqlCommand("SELECT * FROM Reservas WHERE Id = @Id", con);
                cmd.Parameters.AddWithValue("@Id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    reserva = new Reserva
                    {
                        Id = (int)reader["Id"],
                        UsuarioId = (int)reader["UsuarioId"],
                        Fecha = (DateTime)reader["Fecha"],
                        Hora = (TimeSpan)reader["Hora"],
                        Servicios = reader["Servicios"].ToString(),
                        NotificarInterno = (bool)reader["NotificarInterno"]
                    };
                }
            }

            if (reserva == null || reserva.UsuarioId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                TempData["ErrorMessage"] = "Reserva no válida.";
                return RedirectToAction("Reservar");
            }

            return View(reserva);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Elimina y notifica por correo siempre
        public IActionResult EliminarReserva(int id)
        {
            try
            {
                int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                DateTime fecha;
                TimeSpan hora;
                string servicios;
                string pais;

                using (var con = new SqlConnection(_cs))
                {
                    con.Open();

                    using (var checkCmd = new SqlCommand("SELECT UsuarioId, Fecha, Hora, Servicios, Pais FROM Reservas WHERE Id = @Id", con))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", id);
                        using var rd = checkCmd.ExecuteReader();
                        if (!rd.Read() || (int)rd["UsuarioId"] != usuarioId)
                        {
                            TempData["ErrorMessage"] = "No tienes permiso para cancelar esta reserva.";
                            return RedirectToAction("Reservas");
                        }

                        fecha = (DateTime)rd["Fecha"];
                        hora = (TimeSpan)rd["Hora"];
                        servicios = rd["Servicios"].ToString();
                        pais = rd["Pais"].ToString();
                    }

                    using (var cmd = new SqlCommand("DELETE FROM Reservas WHERE Id = @Id", con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                var (nombreUsuario, correoUsuario) = ObtenerNombreYCorreoUsuario(usuarioId);

                // Convertir código de país a nombre completo
                string paisNombreCompleto = ObtenerNombrePais(pais);

                EnviarCorreoUsuario_ReservaCancelada(id, nombreUsuario, correoUsuario, fecha, hora, servicios, paisNombreCompleto);
                EnviarCorreoAdmin_ReservaCancelada(id, nombreUsuario, correoUsuario, fecha, hora, servicios, paisNombreCompleto);

                TempData["ReservaSuccessMessage"] = "Reserva cancelada exitosamente.";
                return RedirectToAction("Reservas");
            }
            catch (Exception ex)
            {
                TempData["ReservaErrorMessage"] = "Error al cancelar la reserva: " + ex.Message;
                return RedirectToAction("Reservas");
            }
        }

        // Lee todas las reservas del usuario autenticado
        public IActionResult MisReservas()
        {
            int usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var reservas = new List<Reserva>();

            using (var con = new SqlConnection(_cs))
            {
                con.Open();
                using var cmd = new SqlCommand(
                    "SELECT * FROM Reservas WHERE UsuarioId = @UsuarioId ORDER BY Fecha DESC", con);
                cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    reservas.Add(new Reserva
                    {
                        Id = (int)reader["Id"],
                        UsuarioId = (int)reader["UsuarioId"],
                        Fecha = (DateTime)reader["Fecha"],
                        Hora = (TimeSpan)reader["Hora"],
                        Servicios = reader["Servicios"].ToString(),
                        NotificarInterno = (bool)reader["NotificarInterno"]
                    });
                }
            }

            return View(reservas);
        }
        // Convierte el código de país (ej. "cr", "mx") en su nombre completo.
        private string ObtenerNombrePais(string codigo)
        {
            return codigo.ToLower() switch
            {
                "cr" => "Costa Rica",
                "tc" => "Turks and Caicos",
                "mx" => "México",
                "us" => "Estados Unidos",
                "bs" => "Bahamas",
                _ => codigo
            };
        }
        // Obtiene el nombre y correo electrónico del usuario desde Claims o la base de datos si no están presentes.
        private (string Nombre, string Correo) ObtenerNombreYCorreoUsuario(int usuarioId)
        {
            string nombreUsuario = User.Identity?.Name ?? "";
            string correoUsuario = User.FindFirstValue(ClaimTypes.Email) ?? "";

            if (!string.IsNullOrWhiteSpace(nombreUsuario) && !string.IsNullOrWhiteSpace(correoUsuario))
                return (nombreUsuario, correoUsuario);

            using var conn = new SqlConnection(_cs);
            conn.Open();
            using var cmd = new SqlCommand(@"
                SELECT TC_Nombre + ' ' + TC_Apellido AS Nombre, TC_Correo_Electronico
                FROM TCCR_A_Usuario
                WHERE TN_Id_Usuario = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", usuarioId);
            using var rd = cmd.ExecuteReader();
            if (rd.Read())
            {
                nombreUsuario = rd.GetString(0);
                correoUsuario = rd.GetString(1);
            }

            return (nombreUsuario, correoUsuario);
        }

        // Métodos individuales para cada tipo de correo al usuario
        private void EnviarCorreoUsuario_ReservaCreada(int reservaId, string nombre, string correo, DateTime fecha, TimeSpan hora, string servicios, string pais)
            => EnviarCorreo(correo,
                "Confirmación de Reserva - Concierge Costa Rica",
                PlantillaUsuario("¡Reserva confirmada!", reservaId, nombre, fecha, hora, servicios, pais,
                "En breve un miembro de nuestro equipo se pondrá en contacto contigo."));
        private void EnviarCorreoUsuario_ReservaActualizada(int reservaId, string nombre, string correo, DateTime fecha, TimeSpan hora, string servicios, string pais)
            => EnviarCorreo(correo,
                "Actualización de Reserva - Concierge Costa Rica",
                PlantillaUsuario("¡Tu reserva fue actualizada!", reservaId, nombre, fecha, hora, servicios, pais,
                "Si no realizaste este cambio, por favor contáctanos."));
        private void EnviarCorreoUsuario_ReservaCancelada(int reservaId, string nombre, string correo, DateTime fecha, TimeSpan hora, string servicios, string pais)
            => EnviarCorreo(correo,
                "Cancelación de Reserva - Concierge Costa Rica",
                PlantillaUsuario("Reserva cancelada", reservaId, nombre, fecha, hora, servicios, pais,
                "Si fue un error, por favor crea una nueva reserva o contáctanos."));

        // Métodos individuales para notificaciones al admin
        private void EnviarCorreoAdmin_ReservaCreada(int reservaId, string nombre, string correo, DateTime fecha, TimeSpan hora, string servicios, string pais)
            => EnviarCorreo(_resOpt.AdminEmail,
                $"[Nueva reserva #{reservaId}] {nombre}",
                PlantillaAdmin("Nueva reserva creada", reservaId, nombre, correo, fecha, hora, servicios, pais));
        private void EnviarCorreoAdmin_ReservaActualizada(int reservaId, string nombre, string correo, DateTime fecha, TimeSpan hora, string servicios, string pais)
            => EnviarCorreo(_resOpt.AdminEmail,
                $"[Reserva actualizada #{reservaId}] {nombre}",
                PlantillaAdmin("Reserva actualizada", reservaId, nombre, correo, fecha, hora, servicios, pais));
        private void EnviarCorreoAdmin_ReservaCancelada(int reservaId, string nombre, string correo, DateTime fecha, TimeSpan hora, string servicios, string pais)
            => EnviarCorreo(_resOpt.AdminEmail,
                $"[Reserva cancelada #{reservaId}] {nombre}",
                PlantillaAdmin("Reserva cancelada por el cliente", reservaId, nombre, correo, fecha, hora, servicios, pais));


        // ===== Motor de envío y plantillas HTML =====
        private void EnviarCorreo(string para, string asunto, string html)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(para)) return;
                _email.SendAsync(para, asunto, html).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                TempData["ReservaErrorMessage"] = (TempData["ReservaErrorMessage"] ?? "").ToString()
                    + $" Problema al enviar correo: {ex.Message}";
            }
        }
        // Plantilla HTML utilizada para correos al usuario, con formato amigable.
        // Incluye fecha, hora, país y servicios.
        private string PlantillaUsuario(string titulo, int id, string nombre, DateTime fecha, TimeSpan hora, string servicios, string pais, string notaFinal)
        {
            var culture = new CultureInfo("es-CR");
            string fechaStr = fecha.ToString("dddd, dd 'de' MMMM yyyy", culture);
            string horaStr = hora.ToString(@"hh\:mm", culture);

            return $@"
        <div style='font-family:Arial,sans-serif;'>
            <h2>{titulo}</h2>
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Detalles de tu reserva:</p>
            <ul>
                <li><strong>No. Reserva:</strong> {id}</li>
                <li><strong>Fecha:</strong> {fechaStr}</li>
                <li><strong>Hora:</strong> {horaStr}</li>
                <li><strong>Servicios:</strong> {servicios}</li>
                <li><strong>País:</strong> {pais}</li>
            </ul>
            <p>{notaFinal}</p>
            <p>— Equipo de Operaciones, Concierge Costa Rica</p>
        </div>";
        }

        // Plantilla HTML para correos al administrador. Incluye correo del cliente.
        private string PlantillaAdmin(string titulo, int id, string nombre, string correo, DateTime fecha, TimeSpan hora, string servicios, string pais)
        {
            var culture = new CultureInfo("es-CR");
            string fechaStr = fecha.ToString("dddd, dd 'de' MMMM yyyy", culture);
            string horaStr = hora.ToString(@"hh\:mm", culture);

            return $@"
        <div style='font-family:Arial,sans-serif;'>
            <h2>{titulo}</h2>
            <p> Reserva hecha con los siguientes datos:</p>
            <ul>
                <li><strong>No. Reserva:</strong> {id}</li>
                <li><strong>Nombre del cliente:</strong> {nombre}</li>
                <li><strong>Correo del cliente:</strong> {correo}</li>
                <li><strong>Fecha:</strong> {fechaStr}</li>
                <li><strong>Hora:</strong> {horaStr}</li>
                <li><strong>Servicios:</strong> {servicios}</li>
                <li><strong>País:</strong> {pais}</li>
            </ul>
            <p>— Sistema Concierge Costa Rica</p>
        </div>";
        }
    }
}