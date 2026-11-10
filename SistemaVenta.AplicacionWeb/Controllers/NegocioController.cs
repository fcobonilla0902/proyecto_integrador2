using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.AplicacionWeb.Utilidades.Response;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class NegocioController : Controller
    {
        private const string RECURSO_NEGOCIO = "Negocio";
        private const string PROPIEDAD_SIMBOLO_MONEDA = "SimboloMoneda";

        private readonly IApiClient _api;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public NegocioController(IApiClient api, IWebHostEnvironment webHostEnvironment)
        {
            _api = api;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ListaRegimenFiscal()
        {
            try
            {
                List<RegimenFiscalSATApiDto>? regimenesApi =
                    await _api.GetAsync<List<RegimenFiscalSATApiDto>>("api/regimenfiscalsat");

                var regimenes = regimenesApi?
                    .Where(x => !string.IsNullOrWhiteSpace(x.C_RegimenFiscal))
                    .OrderBy(x => x.C_RegimenFiscal)
                    .Select(x => new
                    {
                        idRegimenFiscal = x.IdRegimenFiscal,
                        codigo = x.C_RegimenFiscal,
                        descripcion = x.Descripcion,
                        texto = $"{x.C_RegimenFiscal} - {x.Descripcion}"
                    })
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, regimenes ?? new());
            }
            catch
            {
                return StatusCode(StatusCodes.Status200OK, new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListaPaises()
        {
            try
            {
                List<PaisSATApiDto>? paisesApi =
                    await _api.GetAsync<List<PaisSATApiDto>>("api/paissat?skip=0&take=99999");

                var paises = paisesApi?
                    .OrderBy(x => x.Descripcion)
                    .Select(x => new
                    {
                        idPais = x.IdPais,
                        codigo = x.C_Pais,
                        descripcion = x.Descripcion,
                        texto = string.IsNullOrWhiteSpace(x.C_Pais)
                            ? x.Descripcion
                            : $"{x.C_Pais} - {x.Descripcion}"
                    })
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, paises ?? new());
            }
            catch
            {
                return StatusCode(StatusCodes.Status200OK, new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListaEstados()
        {
            try
            {
                List<EstadoSATApiDto>? estadosApi =
                    await _api.GetAsync<List<EstadoSATApiDto>>("api/estadosat?skip=0&take=99999");

                var estados = estadosApi?
                    .OrderBy(x => x.NombreDelEstado)
                    .Select(x => new
                    {
                        idEstado = x.IdEstado,
                        idPais = x.IdPais,
                        codigo = x.C_Estado,
                        descripcion = x.NombreDelEstado,
                        texto = string.IsNullOrWhiteSpace(x.C_Estado)
                            ? x.NombreDelEstado
                            : $"{x.C_Estado} - {x.NombreDelEstado}"
                    })
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, estados ?? new());
            }
            catch
            {
                return StatusCode(StatusCodes.Status200OK, new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListaMunicipios()
        {
            try
            {
                List<MunicipioSATApiDto>? municipiosApi =
                    await _api.GetAsync<List<MunicipioSATApiDto>>("api/municipiosat?skip=0&take=99999");

                var municipios = municipiosApi?
                    .OrderBy(x => x.Descripcion)
                    .Select(x => new
                    {
                        idMunicipio = x.IdMunicipio,
                        idEstado = x.IdEstado,
                        codigo = x.C_Municipio,
                        descripcion = x.Descripcion,
                        texto = string.IsNullOrWhiteSpace(x.C_Municipio)
                            ? x.Descripcion
                            : $"{x.C_Municipio} - {x.Descripcion}"
                    })
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, municipios ?? new());
            }
            catch
            {
                return StatusCode(StatusCodes.Status200OK, new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Obtener()
        {
            GenericResponse<VMNegocio> gResponse = new GenericResponse<VMNegocio>();

            try
            {
                List<NegocioApiDto>? negociosApi = await _api.GetAsync<List<NegocioApiDto>>("api/negocio");
                NegocioApiDto? negocioApi = negociosApi?.FirstOrDefault();

                if (negocioApi == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se encontró información del negocio.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                string? codigoPostal = await ObtenerCodigoPostalTexto(negocioApi.IdCodigoPostal);
                string? regimenFiscal = await ObtenerRegimenFiscalCodigo(negocioApi.IdRegimenFiscal);
                string? pais = await ObtenerPaisTexto(negocioApi.IdPais);
                string? estado = await ObtenerEstadoTexto(negocioApi.IdEstado);
                string? municipio = await ObtenerMunicipioTexto(negocioApi.IdMunicipio);
                string simboloMoneda = await ObtenerSimboloMoneda(negocioApi.IdNegocio);

                VMNegocio vmNegocio = MapearNegocioApiAVm(
                    negocioApi,
                    codigoPostal,
                    regimenFiscal,
                    pais,
                    estado,
                    municipio,
                    simboloMoneda
                );

                gResponse.Estado = true;
                gResponse.Objeto = vmNegocio;
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarCambios([FromForm] IFormFile? logo, [FromForm] string modelo)
        {
            GenericResponse<VMNegocio> gResponse = new GenericResponse<VMNegocio>();

            try
            {
                VMNegocio vmNegocio = JsonConvert.DeserializeObject<VMNegocio>(modelo)!;

                List<NegocioApiDto>? negociosApi = await _api.GetAsync<List<NegocioApiDto>>("api/negocio");
                NegocioApiDto? negocioActual = negociosApi?.FirstOrDefault();

                int? idCodigoPostal = await ObtenerIdCodigoPostalPorCodigo(vmNegocio.CodigoPostal);
                int? idRegimenFiscal = await ObtenerIdRegimenFiscalPorCodigo(vmNegocio.RegimenFiscal);

                if (!string.IsNullOrWhiteSpace(vmNegocio.CodigoPostal) && idCodigoPostal == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = $"No se encontró el código postal {vmNegocio.CodigoPostal} en el catálogo SAT.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                if (!string.IsNullOrWhiteSpace(vmNegocio.RegimenFiscal) && idRegimenFiscal == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = $"No se encontró el régimen fiscal {vmNegocio.RegimenFiscal} en el catálogo SAT.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                string nombreLogo = negocioActual?.NombreLogo ?? "";
                string urlLogo = negocioActual?.UrlLogo ?? "/imagenes/carpeta_logo/default.png";

                if (logo != null && logo.Length > 0)
                {
                    nombreLogo = await GuardarArchivoAsync(logo, "carpeta_logo");
                    urlLogo = $"/imagenes/carpeta_logo/{nombreLogo}";
                }

                NegocioApiDto negocioGuardado;

                if (negocioActual == null)
                {
                    CreaNegocioApiDto dtoCrear = new CreaNegocioApiDto
                    {
                        Nombre = vmNegocio.Nombre,
                        UrlLogo = urlLogo,
                        NombreLogo = nombreLogo,
                        Rfc = vmNegocio.Rfc,
                        Correo = vmNegocio.Correo,
                        Direccion = vmNegocio.Direccion,
                        Telefono = vmNegocio.Telefono,
                        IdCodigoPostal = idCodigoPostal,
                        IdPais = vmNegocio.IdPais,
                        IdEstado = vmNegocio.IdEstado,
                        IdMunicipio = vmNegocio.IdMunicipio,
                        IdRegimenFiscal = idRegimenFiscal
                    };

                    NegocioApiDto? negocioCreado = await _api.PostAsync<NegocioApiDto>("api/negocio", dtoCrear);

                    if (negocioCreado == null)
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "No se pudo crear la información del negocio.";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }

                    negocioGuardado = negocioCreado;
                }
                else
                {
                    NegocioApiDto dtoEditar = new NegocioApiDto
                    {
                        IdNegocio = negocioActual.IdNegocio,
                        Nombre = vmNegocio.Nombre,
                        UrlLogo = urlLogo,
                        NombreLogo = nombreLogo,
                        Rfc = vmNegocio.Rfc,
                        Correo = vmNegocio.Correo,
                        Direccion = vmNegocio.Direccion,
                        Telefono = vmNegocio.Telefono,
                        IdCodigoPostal = idCodigoPostal,
                        IdPais = vmNegocio.IdPais,
                        IdEstado = vmNegocio.IdEstado,
                        IdMunicipio = vmNegocio.IdMunicipio,
                        IdRegimenFiscal = idRegimenFiscal
                    };

                    NegocioApiDto? negocioEditado = await _api.PutAsync<NegocioApiDto>("api/negocio", dtoEditar);

                    if (negocioEditado == null)
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "No se pudo guardar la información del negocio.";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }

                    negocioGuardado = negocioEditado;
                }

                await GuardarSimboloMoneda(
                    negocioGuardado.IdNegocio,
                    string.IsNullOrWhiteSpace(vmNegocio.SimboloMoneda) ? "$" : vmNegocio.SimboloMoneda
                );

                VMNegocio vmGuardado = MapearNegocioApiAVm(
                    negocioGuardado,
                    vmNegocio.CodigoPostal,
                    vmNegocio.RegimenFiscal,
                    vmNegocio.Pais,
                    vmNegocio.EstadoNombre,
                    vmNegocio.Municipio,
                    string.IsNullOrWhiteSpace(vmNegocio.SimboloMoneda) ? "$" : vmNegocio.SimboloMoneda
                );

                gResponse.Estado = true;
                gResponse.Objeto = vmGuardado;
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        private static VMNegocio MapearNegocioApiAVm(
            NegocioApiDto negocioApi,
            string? codigoPostal,
            string? regimenFiscal,
            string? pais,
            string? estado,
            string? municipio,
            string simboloMoneda)
        {
            return new VMNegocio
            {
                IdNegocio = negocioApi.IdNegocio,
                UrlLogo = string.IsNullOrWhiteSpace(negocioApi.UrlLogo)
                    ? "/imagenes/carpeta_logo/default.png"
                    : negocioApi.UrlLogo,
                Rfc = negocioApi.Rfc,
                Nombre = negocioApi.Nombre,
                Correo = negocioApi.Correo,
                Direccion = negocioApi.Direccion,
                Telefono = negocioApi.Telefono,
                CodigoPostal = codigoPostal,
                IdPais = negocioApi.IdPais,
                Pais = pais,
                IdEstado = negocioApi.IdEstado,
                EstadoNombre = estado,
                IdMunicipio = negocioApi.IdMunicipio,
                Municipio = municipio,
                SimboloMoneda = simboloMoneda,
                RegimenFiscal = regimenFiscal
            };
        }

        private async Task<string?> ObtenerCodigoPostalTexto(int? idCodigoPostal)
        {
            if (!idCodigoPostal.HasValue)
                return null;

            try
            {
                CodigoPostalSATApiDto? codigoPostal =
                    await _api.GetAsync<CodigoPostalSATApiDto>($"api/codigopostalsat/{idCodigoPostal.Value}");

                return codigoPostal?.C_CodigoPostal;
            }
            catch
            {
                return idCodigoPostal.Value.ToString();
            }
        }

        private async Task<int?> ObtenerIdCodigoPostalPorCodigo(string? codigoPostal)
        {
            if (string.IsNullOrWhiteSpace(codigoPostal))
                return null;

            List<CodigoPostalSATApiDto>? codigos =
                await _api.GetAsync<List<CodigoPostalSATApiDto>>("api/codigopostalsat?skip=0&take=99999");

            return codigos?
                .FirstOrDefault(x => x.C_CodigoPostal == codigoPostal.Trim())?
                .IdCodigoPostal;
        }

        private async Task<string?> ObtenerRegimenFiscalCodigo(int? idRegimenFiscal)
        {
            if (!idRegimenFiscal.HasValue)
                return null;

            try
            {
                RegimenFiscalSATApiDto? regimen =
                    await _api.GetAsync<RegimenFiscalSATApiDto>($"api/regimenfiscalsat/{idRegimenFiscal.Value}");

                return regimen?.C_RegimenFiscal;
            }
            catch
            {
                return idRegimenFiscal.Value.ToString();
            }
        }

        private async Task<int?> ObtenerIdRegimenFiscalPorCodigo(string? regimenFiscal)
        {
            if (string.IsNullOrWhiteSpace(regimenFiscal))
                return null;

            List<RegimenFiscalSATApiDto>? regimenes =
                await _api.GetAsync<List<RegimenFiscalSATApiDto>>("api/regimenfiscalsat");

            return regimenes?
                .FirstOrDefault(x => x.C_RegimenFiscal == regimenFiscal.Trim())?
                .IdRegimenFiscal;
        }

        private async Task<string?> ObtenerPaisTexto(int? idPais)
        {
            if (!idPais.HasValue)
                return null;

            try
            {
                PaisSATApiDto? pais = await _api.GetAsync<PaisSATApiDto>($"api/paissat/{idPais.Value}");
                return pais?.Descripcion;
            }
            catch
            {
                return idPais.Value.ToString();
            }
        }

        private async Task<string?> ObtenerEstadoTexto(int? idEstado)
        {
            if (!idEstado.HasValue)
                return null;

            try
            {
                EstadoSATApiDto? estado = await _api.GetAsync<EstadoSATApiDto>($"api/estadosat/{idEstado.Value}");
                return estado?.NombreDelEstado;
            }
            catch
            {
                return idEstado.Value.ToString();
            }
        }

        private async Task<string?> ObtenerMunicipioTexto(int? idMunicipio)
        {
            if (!idMunicipio.HasValue)
                return null;

            try
            {
                MunicipioSATApiDto? municipio = await _api.GetAsync<MunicipioSATApiDto>($"api/municipiosat/{idMunicipio.Value}");
                return municipio?.Descripcion;
            }
            catch
            {
                return idMunicipio.Value.ToString();
            }
        }

        private async Task<string> ObtenerSimboloMoneda(int idNegocio)
        {
            try
            {
                List<ConfiguracionApiDto>? configuraciones =
                    await _api.GetAsync<List<ConfiguracionApiDto>>("api/configuracion");

                string? valor = configuraciones?
                    .FirstOrDefault(x =>
                        x.IdNegocio == idNegocio &&
                        string.Equals(x.Recurso, RECURSO_NEGOCIO, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.Propiedad, PROPIEDAD_SIMBOLO_MONEDA, StringComparison.OrdinalIgnoreCase))
                    ?.Valor;

                return string.IsNullOrWhiteSpace(valor) ? "$" : valor;
            }
            catch
            {
                return "$";
            }
        }

        private async Task GuardarSimboloMoneda(int idNegocio, string? simboloMoneda)
        {
            List<ConfiguracionApiDto>? configuraciones =
                await _api.GetAsync<List<ConfiguracionApiDto>>("api/configuracion");

            ConfiguracionApiDto? configuracionActual = configuraciones?
                .FirstOrDefault(x =>
                    x.IdNegocio == idNegocio &&
                    string.Equals(x.Recurso, RECURSO_NEGOCIO, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Propiedad, PROPIEDAD_SIMBOLO_MONEDA, StringComparison.OrdinalIgnoreCase));

            string valor = string.IsNullOrWhiteSpace(simboloMoneda) ? "$" : simboloMoneda;

            if (configuracionActual == null)
            {
                CreaConfiguracionApiDto dtoCrear = new CreaConfiguracionApiDto
                {
                    IdNegocio = idNegocio,
                    Recurso = RECURSO_NEGOCIO,
                    Propiedad = PROPIEDAD_SIMBOLO_MONEDA,
                    Valor = valor,
                    FechaModificacion = DateTime.Now,
                    IdStatusRegistro = 1
                };

                await _api.PostAsync<ConfiguracionApiDto>("api/configuracion", dtoCrear);
            }
            else
            {
                configuracionActual.Valor = valor;
                configuracionActual.FechaModificacion = DateTime.Now;
                configuracionActual.IdStatusRegistro ??= 1;

                await _api.PutAsync<ConfiguracionApiDto>("api/configuracion", configuracionActual);
            }
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

    public class NegocioApiDto
    {
        public int IdNegocio { get; set; }
        public string? UrlLogo { get; set; }
        public string? NombreLogo { get; set; }
        public string? Rfc { get; set; }
        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public int? IdCodigoPostal { get; set; }
        public int? IdPais { get; set; }
        public int? IdEstado { get; set; }
        public int? IdMunicipio { get; set; }
        public int? IdRegimenFiscal { get; set; }
    }

    public class CreaNegocioApiDto
    {
        public string? Nombre { get; set; }
        public string? UrlLogo { get; set; }
        public string? NombreLogo { get; set; }
        public string? Rfc { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public int? IdCodigoPostal { get; set; }
        public int? IdPais { get; set; }
        public int? IdEstado { get; set; }
        public int? IdMunicipio { get; set; }
        public int? IdRegimenFiscal { get; set; }
    }


    public class ConfiguracionApiDto
    {
        public int IdConfiguracion { get; set; }
        public int? IdNegocio { get; set; }
        public string? Recurso { get; set; }
        public string? Propiedad { get; set; }
        public string? Valor { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    public class CreaConfiguracionApiDto
    {
        public int? IdNegocio { get; set; }
        public string? Recurso { get; set; }
        public string? Propiedad { get; set; }
        public string? Valor { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? IdStatusRegistro { get; set; }
    }
}