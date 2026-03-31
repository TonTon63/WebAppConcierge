using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;

// NUEVO: servicio de correo
using WebAppConcierge.Services.Email;

var builder = WebApplication.CreateBuilder(args);

// 1) Localización
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 2) MVC con localización de vistas y DataAnnotations
builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

// 3) Autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });

// 4) Email (SMTP) — lee Smtp y Reservations desde appsettings.json
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<ReservationsOptions>(builder.Configuration.GetSection("Reservations"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

var app = builder.Build();

// 5) Culturas soportadas
var supportedCultures = new[] { new CultureInfo("es-CR"), new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("es-CR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();