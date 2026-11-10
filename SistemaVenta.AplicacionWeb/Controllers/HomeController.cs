using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVenta.AplicacionWeb.Models;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.AplicacionWeb.Utilidades.Response;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IApiClient _api;

        public HomeController(IApiClient api)
        {
            _api = api;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Perfil()
        {
            return View();
        }

        public IActionResult ErrorConexion()
        {
            ViewBag.Titulo = "Sin conexión con la API";
            ViewBag.Mensaje = "No se pudo conectar con el servicio. Verifica que API_Equipo1 esté ejecutándose e intenta nuevamente.";
            return View();
        }

        public IActionResult ErrorStatusCode(int code)
        {
            ViewBag.Codigo = code;
            ViewBag.Mensaje = code switch
            {
                404 => "La página solicitada no existe.",
                403 => "No tienes permisos para acceder a esta sección.",
                _ => "Ocurrió un problema al procesar la solicitud."
            };

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerUsuario()
        {
            GenericResponse<VMUsuario> response = new();

            try
            {
                int idUsuarioApi = ObtenerIdUsuarioApiSesion();

                List<UsuarioApiDto>? usuarios =
                    await _api.GetAsync<List<UsuarioApiDto>>("api/usuario?skip=0&take=99999");

                List<RolApiDto>? roles =
                    await _api.GetAsync<List<RolApiDto>>("api/rol?skip=0&take=99999");

                UsuarioApiDto? usuario = usuarios?
                    .FirstOrDefault(x => x.IdUsuario == idUsuarioApi);

                if (usuario == null)
                {
                    response.Estado = false;
                    response.Mensaje = $"No se encontró el usuario con IdUsuarioApi {idUsuarioApi} en la API.";
                    return StatusCode(StatusCodes.Status200OK, response);
                }

                response.Estado = true;
                response.Objeto = new VMUsuario
                {
                    IdUsuario = usuario.IdUsuario,
                    Nombre = usuario.Nombre,
                    Correo = usuario.Correo,
                    Telefono = usuario.Telefono,
                    IdRol = usuario.IdRol,
                    NombreRol = roles?.FirstOrDefault(r => r.IdRol == usuario.IdRol)?.Descripcion ?? "",
                    UrlFoto = string.IsNullOrWhiteSpace(usuario.UrlFoto)
                        ? "/imagenes/carpeta_usuario/default.png"
                        : usuario.UrlFoto,
                    EsActivo = usuario.IdStatusRegistro
                };
            }
            catch (Exception ex)
            {
                response.Estado = false;
                response.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, response);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarPerfil([FromBody] VMUsuario modelo)
        {
            GenericResponse<VMUsuario> response = new();

            try
            {
                int idUsuarioApi = ObtenerIdUsuarioApiSesion();

                List<UsuarioApiDto>? usuarios =
                    await _api.GetAsync<List<UsuarioApiDto>>("api/usuario?skip=0&take=99999");

                UsuarioApiDto? usuarioActual = usuarios?
                    .FirstOrDefault(x => x.IdUsuario == idUsuarioApi);

                if (usuarioActual == null)
                {
                    response.Estado = false;
                    response.Mensaje = "No se encontró el usuario a editar.";
                    return StatusCode(StatusCodes.Status200OK, response);
                }

                UsuarioApiDto dto = new()
                {
                    IdUsuario = usuarioActual.IdUsuario,
                    Nombre = usuarioActual.Nombre,
                    Correo = modelo.Correo,
                    Telefono = modelo.Telefono,
                    IdRol = usuarioActual.IdRol,
                    UrlFoto = usuarioActual.UrlFoto,
                    NombreFoto = usuarioActual.NombreFoto,
                    Clave = usuarioActual.Clave,
                    FechaRegistro = usuarioActual.FechaRegistro,
                    IdStatusRegistro = usuarioActual.IdStatusRegistro
                };

                UsuarioApiDto? editado =
                    await _api.PutAsync<UsuarioApiDto>("api/usuario", dto);

                response.Estado = editado != null;
                response.Objeto = modelo;
            }
            catch (Exception ex)
            {
                response.Estado = false;
                response.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, response);
        }

        [HttpPost]
        public async Task<IActionResult> CambiarClave([FromBody] VMCambiarClave modelo)
        {
            GenericResponse<bool> response = new();

            try
            {
                int idUsuarioApi = ObtenerIdUsuarioApiSesion();

                List<UsuarioApiDto>? usuarios =
                    await _api.GetAsync<List<UsuarioApiDto>>("api/usuario?skip=0&take=99999");

                UsuarioApiDto? usuarioActual = usuarios?
                    .FirstOrDefault(x => x.IdUsuario == idUsuarioApi);

                if (usuarioActual == null)
                {
                    response.Estado = false;
                    response.Mensaje = "No se encontró el usuario.";
                    return StatusCode(StatusCodes.Status200OK, response);
                }

                string claveActualEncriptada = ConvertirSha256(modelo.claveActual ?? "");

                if (usuarioActual.Clave != claveActualEncriptada)
                {
                    response.Estado = false;
                    response.Mensaje = "La contraseña actual no es correcta.";
                    return StatusCode(StatusCodes.Status200OK, response);
                }

                UsuarioApiDto dto = new()
                {
                    IdUsuario = usuarioActual.IdUsuario,
                    Nombre = usuarioActual.Nombre,
                    Correo = usuarioActual.Correo,
                    Telefono = usuarioActual.Telefono,
                    IdRol = usuarioActual.IdRol,
                    UrlFoto = usuarioActual.UrlFoto,
                    NombreFoto = usuarioActual.NombreFoto,
                    Clave = ConvertirSha256(modelo.claveNueva ?? ""),
                    FechaRegistro = usuarioActual.FechaRegistro,
                    IdStatusRegistro = usuarioActual.IdStatusRegistro
                };

                UsuarioApiDto? editado =
                    await _api.PutAsync<UsuarioApiDto>("api/usuario", dto);

                response.Estado = editado != null;
            }
            catch (Exception ex)
            {
                response.Estado = false;
                response.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, response);
        }

        private int ObtenerIdUsuarioApiSesion()
        {
            string? idUsuarioApi = HttpContext.User.Claims
                .Where(c => c.Type == "IdUsuarioApi")
                .Select(c => c.Value)
                .SingleOrDefault();

            if (!string.IsNullOrWhiteSpace(idUsuarioApi))
            {
                return int.Parse(idUsuarioApi);
            }

            string? idUsuario = HttpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.NameIdentifier)
                .Select(c => c.Value)
                .SingleOrDefault();

            return int.Parse(idUsuario!);
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Acceso");
        }
    }
}