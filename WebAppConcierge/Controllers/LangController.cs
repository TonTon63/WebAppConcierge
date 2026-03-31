using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

public class LangController : Controller
{
    /// Establece la cultura (idioma y formato regional) del sitio mediante la cookie de localización.
    /// La cookie es leída por RequestLocalizationMiddleware en cada request.
    [HttpGet]
    public IActionResult Set(string culture, string returnUrl = "/")
    {
        var allowed = new[] { "es-CR", "en-US" };
        if (!allowed.Contains(culture))
            culture = "es-CR";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
    }
}
