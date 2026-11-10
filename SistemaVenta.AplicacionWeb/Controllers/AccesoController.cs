using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.BLL.Interfaces;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    public class AccesoController : Controller
    {
        private readonly IApiClient _api;
        private readonly IUsuarioService _usuarioServicio;

        public AccesoController(IApiClient api, IUsuarioService usuarioServicio)
        {
            _api = api;
            _usuarioServicio = usuarioServicio;
        }

        public IActionResult Login()
        {
            ClaimsPrincipal claimUser = HttpContext.User;

            if (claimUser.Identity != null && claimUser.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        public IActionResult RestablecerClave()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(VMUsuarioLogin modelo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modelo.Correo) || string.IsNullOrWhiteSpace(modelo.Clave))
                {
                    ViewData["Mensaje"] = "Debe ingresar correo y contraseña";
                    return View();
                }

                List<UsuarioApiDto>? usuarios =
                    await _api.GetAsync<List<UsuarioApiDto>>("api/usuario?skip=0&take=99999");

                usuarios ??= new List<UsuarioApiDto>();

                string claveEncriptada = ConvertirSha256(modelo.Clave.Trim());

                UsuarioApiDto? usuarioEncontrado = usuarios.FirstOrDefault(u =>
                    !string.IsNullOrWhiteSpace(u.Correo) &&
                    u.Correo.Trim().Equals(modelo.Correo.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    (u.Clave ?? "") == claveEncriptada &&
                    (u.IdStatusRegistro ?? 0) == 1
                );

                if (usuarioEncontrado == null)
                {
                    ViewData["Mensaje"] = "No se encontraron coincidencias";
                    return View();
                }

                ViewData["Mensaje"] = null;

                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.Name, usuarioEncontrado.Nombre ?? ""),
                    new Claim(ClaimTypes.NameIdentifier, usuarioEncontrado.IdUsuario.ToString()),
                    new Claim("IdUsuarioApi", usuarioEncontrado.IdUsuario.ToString()),
                    new Claim(ClaimTypes.Role, (usuarioEncontrado.IdRol ?? 0).ToString()),
                    new Claim(ClaimTypes.Email, usuarioEncontrado.Correo ?? ""),
                    new Claim("Correo", usuarioEncontrado.Correo ?? ""),
                    new Claim("UrlFoto", string.IsNullOrWhiteSpace(usuarioEncontrado.UrlFoto)
                        ? "/imagenes/carpeta_usuario/default.png"
                        : usuarioEncontrado.UrlFoto)
                };

                ClaimsIdentity claimsIdentity = new(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                AuthenticationProperties properties = new()
                {
                    AllowRefresh = true,
                    IsPersistent = modelo.MantenerSesion
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    properties
                );

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewData["Mensaje"] = ex.Message;
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> RestablecerClave(VMUsuarioLogin modelo)
        {
            try
            {
                string urlPlantillaCorreo =
                    $"{Request.Scheme}://{Request.Host}/Plantilla/RestablecerClave?clave=[clave]";

                bool resultado = await _usuarioServicio.RestablecerClave(
                    modelo.Correo,
                    urlPlantillaCorreo
                );

                if (resultado)
                {
                    ViewData["Mensaje"] = "Listo, su contraseña fue restablecida. Revise su correo";
                    ViewData["MensajeError"] = null;
                }
                else
                {
                    ViewData["MensajeError"] = "Tenemos problemas. Por favor inténtelo de nuevo más tarde";
                    ViewData["Mensaje"] = null;
                }
            }
            catch (Exception ex)
            {
                ViewData["MensajeError"] = ex.Message;
                ViewData["Mensaje"] = null;
            }

            return View();
        }

        private static string ConvertirSha256(string texto)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
            StringBuilder builder = new();

            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }

        public class UsuarioApiDto
        {
            public int IdUsuario { get; set; }
            public string? Nombre { get; set; }
            public string? Correo { get; set; }
            public string? Telefono { get; set; }
            public int? IdRol { get; set; }
            public string? UrlFoto { get; set; }
            public string? NombreFoto { get; set; }
            public string? Clave { get; set; }
            public DateTime? FechaRegistro { get; set; }
            public int? IdStatusRegistro { get; set; }
        }
    }
}