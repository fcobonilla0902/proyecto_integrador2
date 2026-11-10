using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.AplicacionWeb.Utilidades.Response;
using SistemaVenta.BLL.Interfaces;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly IApiClient _api;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ICorreoService _correoService;

        public UsuarioController(
    IApiClient api,
    IWebHostEnvironment webHostEnvironment,
    ICorreoService correoService)
        {
            _api = api;
            _webHostEnvironment = webHostEnvironment;
            _correoService = correoService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ListaRoles()
        {
            GenericResponse<List<VMRol>> gResponse = new();

            try
            {
                List<RolApiDto>? rolesApi = await _api.GetAsync<List<RolApiDto>>("api/rol");

                gResponse.Estado = true;
                gResponse.Objeto = rolesApi?
                    .Select(x => new VMRol
                    {
                        IdRol = x.IdRol,
                        Descripcion = x.Descripcion
                    })
                    .ToList() ?? new List<VMRol>();
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.";
                gResponse.Objeto = new List<VMRol>();
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpGet]
        public async Task<IActionResult> Lista()
        {
            GenericResponse<List<VMUsuario>> gResponse = new();

            try
            {
                List<UsuarioApiDto>? usuariosApi = await _api.GetAsync<List<UsuarioApiDto>>("api/usuario");
                List<RolApiDto>? rolesApi = await _api.GetAsync<List<RolApiDto>>("api/rol");

                gResponse.Estado = true;
                gResponse.Objeto = usuariosApi?
                    .Select(x => new VMUsuario
                    {
                        IdUsuario = x.IdUsuario,
                        Nombre = x.Nombre,
                        Correo = x.Correo,
                        Telefono = x.Telefono,
                        IdRol = x.IdRol,
                        NombreRol = rolesApi?.FirstOrDefault(r => r.IdRol == x.IdRol)?.Descripcion ?? "",
                        UrlFoto = string.IsNullOrWhiteSpace(x.UrlFoto)
                            ? "/imagenes/sistema/default-user.png"
                            : x.UrlFoto,
                        EsActivo = x.IdStatusRegistro
                    })
                    .ToList() ?? new List<VMUsuario>();
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.";
                gResponse.Objeto = new List<VMUsuario>();
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromForm] IFormFile? foto, [FromForm] string modelo)
        {
            GenericResponse<VMUsuario> gResponse = new GenericResponse<VMUsuario>();

            try
            {
                VMUsuario vmUsuario = JsonConvert.DeserializeObject<VMUsuario>(modelo)!;

                string nombreFoto = "";
                string urlFoto = "/imagenes/sistema/default-user.png";

                if (foto != null && foto.Length > 0)
                {
                    nombreFoto = await GuardarArchivoAsync(foto, "carpeta_usuario");
                    urlFoto = $"/imagenes/carpeta_usuario/{nombreFoto}";
                }

                string claveTemporal = GenerarClaveTemporal();
                string claveHasheada = ConvertirSha256(claveTemporal);

                CreaUsuarioApiDto dto = new CreaUsuarioApiDto
                {
                    Nombre = vmUsuario.Nombre,
                    Correo = vmUsuario.Correo,
                    Telefono = vmUsuario.Telefono,
                    IdRol = vmUsuario.IdRol,
                    UrlFoto = urlFoto,
                    NombreFoto = nombreFoto,
                    Clave = claveHasheada,
                    FechaRegistro = DateTime.Now,
                    IdStatusRegistro = vmUsuario.EsActivo ?? 1
                };

                UsuarioApiDto? usuarioCreado = await _api.PostAsync<UsuarioApiDto>("api/usuario", dto);

                if (usuarioCreado == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se pudo crear el usuario.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                RolApiDto? rol = null;

                if (usuarioCreado.IdRol.HasValue)
                {
                    rol = await _api.GetAsync<RolApiDto>($"api/rol/{usuarioCreado.IdRol.Value}");
                }

                string urlSistema = $"{Request.Scheme}://{Request.Host}/Acceso/Login";

                string asunto = "Credenciales de acceso al sistema";
                string mensaje = $@"
        <div style='background-color:#EDF6FF;padding:30px'>
            <div style='width:400px;border-radius:5px;margin:auto;background-color:#fff;box-shadow:0px 0px 10px #DEDEDE;padding:20px'>
                <h2 style='color:#004DAF;text-align:center;'>Bienvenido</h2>
                <p>Se creó exitosamente tu usuario. Los detalles de tu cuenta son:</p>

                <p><strong>Correo:</strong> {usuarioCreado.Correo}</p>
                <p><strong>Contraseña:</strong> {claveTemporal}</p>

                <div style='background-color:#FFE1CE;padding:15px;margin-top:15px;margin-bottom:15px'>
                    <p style='margin:0px;color:#F45E00;'>
                        Le recomendamos cambiar la contraseña una vez inicie sesión.
                    </p>
                </div>

                <p>Para iniciar sesión ingrese a la siguiente URL:</p>
                <a href='{urlSistema}' target='_blank'>Iniciar Sesión</a>
            </div>
        </div>";

                bool correoEnviado = false;

                if (!string.IsNullOrWhiteSpace(usuarioCreado.Correo))
                {
                    correoEnviado = await _correoService.EnviarCorreo(usuarioCreado.Correo, asunto, mensaje);
                }

                gResponse.Estado = true;
                gResponse.Objeto = new VMUsuario
                {
                    IdUsuario = usuarioCreado.IdUsuario,
                    Nombre = usuarioCreado.Nombre,
                    Correo = usuarioCreado.Correo,
                    Telefono = usuarioCreado.Telefono,
                    IdRol = usuarioCreado.IdRol,
                    NombreRol = rol?.Descripcion ?? "",
                    UrlFoto = string.IsNullOrWhiteSpace(usuarioCreado.UrlFoto)
                        ? "/imagenes/sistema/default-user.png"
                        : usuarioCreado.UrlFoto,
                    EsActivo = usuarioCreado.IdStatusRegistro
                };

                if (!correoEnviado)
                {
                    gResponse.Mensaje = "Usuario creado correctamente, pero no se pudo enviar el correo con la contraseña.";
                }
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpPut]
        public async Task<IActionResult> Editar([FromForm] IFormFile? foto, [FromForm] string modelo)
        {
            GenericResponse<VMUsuario> gResponse = new GenericResponse<VMUsuario>();

            try
            {
                VMUsuario vmUsuario = JsonConvert.DeserializeObject<VMUsuario>(modelo)!;

                UsuarioApiDto? usuarioActual = await _api.GetAsync<UsuarioApiDto>($"api/usuario/{vmUsuario.IdUsuario}");

                if (usuarioActual == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se encontró el usuario a editar.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                string nombreFoto = usuarioActual.NombreFoto ?? "";
                string urlFoto = usuarioActual.UrlFoto ?? "/imagenes/sistema/default-user.png";

                if (foto != null && foto.Length > 0)
                {
                    nombreFoto = await GuardarArchivoAsync(foto, "carpeta_usuario");
                    urlFoto = $"/imagenes/carpeta_usuario/{nombreFoto}";
                }

                UsuarioApiDto dto = new UsuarioApiDto
                {
                    IdUsuario = vmUsuario.IdUsuario,
                    Nombre = vmUsuario.Nombre,
                    Correo = vmUsuario.Correo,
                    Telefono = vmUsuario.Telefono,
                    IdRol = vmUsuario.IdRol,
                    UrlFoto = urlFoto,
                    NombreFoto = nombreFoto,
                    Clave = usuarioActual.Clave,
                    FechaRegistro = usuarioActual.FechaRegistro,
                    IdStatusRegistro = vmUsuario.EsActivo ?? 1
                };

                UsuarioApiDto? usuarioEditado = await _api.PutAsync<UsuarioApiDto>("api/usuario", dto);

                if (usuarioEditado == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se pudo editar el usuario.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }
                int idUsuarioSesion = ObtenerIdUsuarioApiSesion();

                if (usuarioEditado.IdUsuario == idUsuarioSesion)
                {
                    await RefrescarSesionUsuarioAsync(usuarioEditado);
                }

                RolApiDto? rol = null;

                if (usuarioEditado.IdRol.HasValue)
                {
                    rol = await _api.GetAsync<RolApiDto>($"api/rol/{usuarioEditado.IdRol.Value}");
                }

                gResponse.Estado = true;
                gResponse.Objeto = new VMUsuario
                {
                    IdUsuario = usuarioEditado.IdUsuario,
                    Nombre = usuarioEditado.Nombre,
                    Correo = usuarioEditado.Correo,
                    Telefono = usuarioEditado.Telefono,
                    IdRol = usuarioEditado.IdRol,
                    NombreRol = rol?.Descripcion ?? "",
                    UrlFoto = string.IsNullOrWhiteSpace(usuarioEditado.UrlFoto)
                        ? "/imagenes/sistema/default-user.png"
                        : usuarioEditado.UrlFoto,
                    EsActivo = usuarioEditado.IdStatusRegistro
                };
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpDelete]
        public async Task<IActionResult> Eliminar(int IdUsuario)
        {
            GenericResponse<string> gResponse = new GenericResponse<string>();

            try
            {
                gResponse.Estado = await _api.DeleteAsync($"api/usuario/{IdUsuario}");

                if (!gResponse.Estado)
                {
                    gResponse.Mensaje = "No se pudo eliminar el usuario.";
                }
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
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
        private string GenerarClaveTemporal()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789@$!%*?";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private async Task RefrescarSesionUsuarioAsync(UsuarioApiDto usuarioEditado)
        {
            ClaimsPrincipal claimUser = HttpContext.User;
            ClaimsIdentity identityActual = (ClaimsIdentity)claimUser.Identity!;

            List<Claim> claimsActualizados = identityActual.Claims
                .Where(c => c.Type != ClaimTypes.Name && c.Type != "UrlFoto")
                .ToList();

            claimsActualizados.Add(new Claim(ClaimTypes.Name, usuarioEditado.Nombre ?? ""));
            claimsActualizados.Add(new Claim("UrlFoto",
                string.IsNullOrWhiteSpace(usuarioEditado.UrlFoto)
                    ? "/imagenes/sistema/default-user.png"
                    : usuarioEditado.UrlFoto));

            ClaimsIdentity nuevaIdentity = new ClaimsIdentity(
                claimsActualizados,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            ClaimsPrincipal nuevoPrincipal = new ClaimsPrincipal(nuevaIdentity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                nuevoPrincipal
            );
        }

        private static string ConvertirSha256(string texto)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
            StringBuilder builder = new StringBuilder();

            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
        private async Task<string> GuardarArchivoAsync(IFormFile archivo, string carpeta)
        {
            string nombreArchivo = $"{Guid.NewGuid():N}{Path.GetExtension(archivo.FileName)}";

            string rutaCarpeta = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "imagenes",
                carpeta
            );

            if (!Directory.Exists(rutaCarpeta))
            {
                Directory.CreateDirectory(rutaCarpeta);
            }

            string rutaArchivo = Path.Combine(rutaCarpeta, nombreArchivo);

            using FileStream stream = new FileStream(rutaArchivo, FileMode.Create);
            await archivo.CopyToAsync(stream);

            return nombreArchivo;
        }
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

    public class CreaUsuarioApiDto
    {
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

    public class RolApiDto
    {
        public int IdRol { get; set; }
        public string? Descripcion { get; set; }
        public int? IdStatusRegistro { get; set; }
        public DateTime? FechaRegistro { get; set; }
    }
}