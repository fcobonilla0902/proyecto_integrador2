using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.CodeAnalysis;
using SistemaVenta.AplicacionWeb.Utilidades.Automapper;
using SistemaVenta.AplicacionWeb.Utilidades.Extensiones;
using SistemaVenta.BLL.Implementacion;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.IOC;
using SistemaVenta.AplicacionWeb.Services.Api;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]!;

builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(option =>
    {
        option.LoginPath = "/Acceso/Login";
        option.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    });

builder.Services.InyectarDependencia(builder.Configuration);

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(AutoMapperProfile).Assembly);
});

var context = new CustomAssemblyLoadContext();
context.LoadUnmanagedLibrary(Path.Combine(Directory.GetCurrentDirectory(), "Utilidades/LibreriaPDF/libwkhtmltox.dll"));
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
builder.Services.AddScoped<ICorreoService, CorreoService>();

builder.Services.AddScoped<ILocalStorageService>(provider =>
{
    var env = provider.GetRequiredService<IWebHostEnvironment>();
    return new LocalStorageService(env.WebRootPath);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/ErrorConexion");
    app.UseStatusCodePagesWithReExecute("/Home/ErrorStatusCode", "?code={0}");
}
else
{
    app.UseExceptionHandler("/Home/ErrorConexion");
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Facturas") &&
        !new[] { ".pdf", ".xml" }.Contains(Path.GetExtension(context.Request.Path.Value ?? ""), StringComparer.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
