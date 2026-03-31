using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebAppConcierge.Models;

namespace WebAppConcierge.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        //Retorna la vista principal del sitio 
        public IActionResult Index()
        {
            return View();
        }

        //Muestra la vista de politica de privacidad del sitio
        public IActionResult Privacy()
        {
            return View();
        }

        //Muestra una lista o descripcion de los servicios ofrecidos.
        public IActionResult ConciergeServices()
        {
            return View();
        }

        //Muestra las ubicaciones disponibles para los servicios ofrecidos
        public IActionResult Locations()
        {
            return View();
        }

        //Muestra una vista de error generica con el RequestId
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //Muestra la vista de contacto
        public IActionResult Contactar() => View();

        //Carga la vista de preguntas frecuentes FAQ
        public IActionResult Preguntas() => View();

        //Muestra la vista de Sobre Nosotros donde se expongo la informacion sobre la empresa o equipo
        public IActionResult Sobre() => View();

        public IActionResult Reservas()
        {
            return View();
        }
    }
}

