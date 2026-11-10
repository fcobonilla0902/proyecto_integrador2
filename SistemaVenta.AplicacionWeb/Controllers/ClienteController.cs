using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.AplicacionWeb.Utilidades.Response;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class ClienteController : Controller
    {
        private readonly IApiClient _api;

        public ClienteController(IApiClient api)
        {
            _api = api;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region Catálogos SAT

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

        #endregion

        #region Listado principal

        [HttpGet]
        public async Task<IActionResult> Lista()
        {
            GenericResponse<List<VMCliente>> gResponse = new();

            try
            {
                List<ClienteApiDto>? clientesApi =
                    await _api.GetAsync<List<ClienteApiDto>>("api/cliente?skip=0&take=99999");

                List<CodigoPostalSATApiDto>? codigosPostales =
                    await _api.GetAsync<List<CodigoPostalSATApiDto>>("api/codigopostalsat?skip=0&take=99999");

                List<RegimenFiscalSATApiDto>? regimenes =
                    await _api.GetAsync<List<RegimenFiscalSATApiDto>>("api/regimenfiscalsat");

                List<PaisSATApiDto>? paises =
                    await _api.GetAsync<List<PaisSATApiDto>>("api/paissat?skip=0&take=99999");

                List<EstadoSATApiDto>? estados =
                    await _api.GetAsync<List<EstadoSATApiDto>>("api/estadosat?skip=0&take=99999");

                List<MunicipioSATApiDto>? municipios =
                    await _api.GetAsync<List<MunicipioSATApiDto>>("api/municipiosat?skip=0&take=99999");

                gResponse.Estado = true;
                gResponse.Objeto = clientesApi?
                    .Select(x =>
                    {
                        string? codigoPostal = codigosPostales?
                            .FirstOrDefault(cp => cp.IdCodigoPostal == x.IdCodigoPostal)?
                            .C_CodigoPostal;

                        string? regimenFiscal = regimenes?
                            .FirstOrDefault(r => r.IdRegimenFiscal == x.IdRegimenFiscal)?
                            .C_RegimenFiscal;

                        PaisSATApiDto? pais = paises?.FirstOrDefault(p => p.IdPais == x.IdPais);
                        EstadoSATApiDto? estado = estados?.FirstOrDefault(e => e.IdEstado == x.IdEstado);
                        MunicipioSATApiDto? municipio = municipios?.FirstOrDefault(m => m.IdMunicipio == x.IdMunicipio);

                        return new VMCliente
                        {
                            IdCliente = x.IdCliente,
                            Nombre = x.NombreCliente,
                            Correo = x.CorreoElectronico,
                            Rfc = x.RfcCliente,
                            DireccionFiscal = x.DireccionFiscal,
                            CodigoPostal = codigoPostal,
                            DomicilioFiscalReceptor = codigoPostal,
                            RegimenFiscalReceptor = regimenFiscal,
                            IdPais = x.IdPais,
                            Pais = pais?.Descripcion,
                            IdEstado = x.IdEstado,
                            EstadoNombre = estado?.NombreDelEstado,
                            IdMunicipio = x.IdMunicipio,
                            Municipio = municipio?.Descripcion,
                            EsActivo = x.IdStatusRegistro,
                            FechaRegistro = x.FechaRegistro ?? DateTime.Now
                        };
                    })
                    .ToList() ?? new List<VMCliente>();
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.";
                gResponse.Objeto = new List<VMCliente>();
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        #endregion

        #region Crear / Editar / Eliminar

        [HttpPost]
        public async Task<IActionResult> Crear([FromForm] string modelo)
        {
            GenericResponse<VMCliente> gResponse = new GenericResponse<VMCliente>();

            try
            {
                VMCliente vmCliente = JsonConvert.DeserializeObject<VMCliente>(modelo)!;

                int? idCodigoPostal = await ObtenerIdCodigoPostalPorCodigo(vmCliente.CodigoPostal);
                int? idRegimenFiscal = await ObtenerIdRegimenFiscalPorCodigo(vmCliente.RegimenFiscalReceptor);

                if (!string.IsNullOrWhiteSpace(vmCliente.CodigoPostal) && idCodigoPostal == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = $"No se encontró el código postal {vmCliente.CodigoPostal} en el catálogo SAT.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                if (!string.IsNullOrWhiteSpace(vmCliente.RegimenFiscalReceptor) && idRegimenFiscal == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = $"No se encontró el régimen fiscal {vmCliente.RegimenFiscalReceptor} en el catálogo SAT.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                CreaClienteApiDto dto = new CreaClienteApiDto
                {
                    NombreCliente = vmCliente.Nombre,
                    CorreoElectronico = vmCliente.Correo,
                    RfcCliente = vmCliente.Rfc,
                    DireccionFiscal = vmCliente.DireccionFiscal,
                    IdCodigoPostal = idCodigoPostal,
                    IdRegimenFiscal = idRegimenFiscal,
                    IdPais = vmCliente.IdPais,
                    IdEstado = vmCliente.IdEstado,
                    IdMunicipio = vmCliente.IdMunicipio,
                    IdStatusRegistro = vmCliente.EsActivo ?? 1,
                    FechaRegistro = DateTime.Now
                };

                ClienteApiDto? creado = await _api.PostAsync<ClienteApiDto>("api/cliente", dto);

                if (creado == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se pudo crear el cliente.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                gResponse.Estado = true;
                gResponse.Objeto = await MapearClienteApiAVm(creado);
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpPut]
        public async Task<IActionResult> Editar([FromForm] string modelo)
        {
            GenericResponse<VMCliente> gResponse = new GenericResponse<VMCliente>();

            try
            {
                VMCliente vmCliente = JsonConvert.DeserializeObject<VMCliente>(modelo)!;

                ClienteApiDto? clienteActual =
                    await _api.GetAsync<ClienteApiDto>($"api/cliente/{vmCliente.IdCliente}");

                if (clienteActual == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se encontró el cliente a editar.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                int? idCodigoPostal = await ObtenerIdCodigoPostalPorCodigo(vmCliente.CodigoPostal);
                int? idRegimenFiscal = await ObtenerIdRegimenFiscalPorCodigo(vmCliente.RegimenFiscalReceptor);

                if (!string.IsNullOrWhiteSpace(vmCliente.CodigoPostal) && idCodigoPostal == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = $"No se encontró el código postal {vmCliente.CodigoPostal} en el catálogo SAT.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                if (!string.IsNullOrWhiteSpace(vmCliente.RegimenFiscalReceptor) && idRegimenFiscal == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = $"No se encontró el régimen fiscal {vmCliente.RegimenFiscalReceptor} en el catálogo SAT.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                ClienteApiDto dto = new ClienteApiDto
                {
                    IdCliente = vmCliente.IdCliente,
                    NombreCliente = vmCliente.Nombre,
                    CorreoElectronico = vmCliente.Correo,
                    RfcCliente = vmCliente.Rfc,
                    DireccionFiscal = vmCliente.DireccionFiscal,
                    IdCodigoPostal = idCodigoPostal,
                    IdRegimenFiscal = idRegimenFiscal,
                    IdPais = vmCliente.IdPais,
                    IdEstado = vmCliente.IdEstado,
                    IdMunicipio = vmCliente.IdMunicipio,
                    FechaRegistro = clienteActual.FechaRegistro,
                    IdStatusRegistro = vmCliente.EsActivo ?? 1
                };

                ClienteApiDto? editado = await _api.PutAsync<ClienteApiDto>("api/cliente", dto);

                if (editado == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se pudo editar el cliente.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                gResponse.Estado = true;
                gResponse.Objeto = await MapearClienteApiAVm(editado);
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpDelete]
        public async Task<IActionResult> Eliminar(int idCLiente)
        {
            GenericResponse<string> gResponse = new GenericResponse<string>();

            try
            {
                gResponse.Estado = await _api.DeleteAsync($"api/cliente/{idCLiente}");

                if (!gResponse.Estado)
                {
                    gResponse.Mensaje = "No se pudo eliminar el cliente.";
                }
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        #endregion

        #region Utilidades de mapeo

        private async Task<VMCliente> MapearClienteApiAVm(ClienteApiDto clienteApi)
        {
            string? codigoPostal = await ObtenerCodigoPostalTexto(clienteApi.IdCodigoPostal);
            string? regimenFiscal = await ObtenerRegimenFiscalCodigo(clienteApi.IdRegimenFiscal);
            string? pais = await ObtenerPaisTexto(clienteApi.IdPais);
            string? estado = await ObtenerEstadoTexto(clienteApi.IdEstado);
            string? municipio = await ObtenerMunicipioTexto(clienteApi.IdMunicipio);

            return new VMCliente
            {
                IdCliente = clienteApi.IdCliente,
                Nombre = clienteApi.NombreCliente,
                Correo = clienteApi.CorreoElectronico,
                Rfc = clienteApi.RfcCliente,
                DireccionFiscal = clienteApi.DireccionFiscal,
                CodigoPostal = codigoPostal,
                DomicilioFiscalReceptor = codigoPostal,
                RegimenFiscalReceptor = regimenFiscal,
                IdPais = clienteApi.IdPais,
                Pais = pais,
                IdEstado = clienteApi.IdEstado,
                EstadoNombre = estado,
                IdMunicipio = clienteApi.IdMunicipio,
                Municipio = municipio,
                EsActivo = clienteApi.IdStatusRegistro,
                FechaRegistro = clienteApi.FechaRegistro ?? DateTime.Now
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
                MunicipioSATApiDto? municipio =
                    await _api.GetAsync<MunicipioSATApiDto>($"api/municipiosat/{idMunicipio.Value}");
                return municipio?.Descripcion;
            }
            catch
            {
                return idMunicipio.Value.ToString();
            }
        }

        #endregion

        #region Búsqueda rápida (auto-complete, modal, etc.)

        [HttpGet]
        public async Task<IActionResult> BuscarClientes(string busqueda)
        {
            try
            {
                var clientes = await _api.GetAsync<List<ClienteApiDto>>("api/cliente?skip=0&take=99999");
                clientes ??= new List<ClienteApiDto>();

                List<CodigoPostalSATApiDto>? codigosPostales =
                    await _api.GetAsync<List<CodigoPostalSATApiDto>>("api/codigopostalsat?skip=0&take=99999");

                List<RegimenFiscalSATApiDto>? regimenes =
                    await _api.GetAsync<List<RegimenFiscalSATApiDto>>("api/regimenfiscalsat");

                List<PaisSATApiDto>? paises =
                    await _api.GetAsync<List<PaisSATApiDto>>("api/paissat?skip=0&take=99999");

                List<EstadoSATApiDto>? estados =
                    await _api.GetAsync<List<EstadoSATApiDto>>("api/estadosat?skip=0&take=99999");

                List<MunicipioSATApiDto>? municipios =
                    await _api.GetAsync<List<MunicipioSATApiDto>>("api/municipiosat?skip=0&take=99999");

                var textoBusqueda = (busqueda ?? string.Empty).Trim();

                var resultado = clientes
                    .Where(c =>
                        string.IsNullOrWhiteSpace(textoBusqueda) ||
                        (!string.IsNullOrWhiteSpace(c.NombreCliente) && c.NombreCliente.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(c.RfcCliente) && c.RfcCliente.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(c.CorreoElectronico) && c.CorreoElectronico.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase))
                    )
                    .Take(20)
                    .Select(c =>
                    {
                        string? codigoPostal = codigosPostales?
                            .FirstOrDefault(cp => cp.IdCodigoPostal == c.IdCodigoPostal)?
                            .C_CodigoPostal;

                        return new
                        {
                            idCliente = c.IdCliente,
                            nombreCompleto = c.NombreCliente,
                            correo = c.CorreoElectronico,
                            rfc = c.RfcCliente,
                            domicilioFiscal = c.DireccionFiscal,
                            domicilioFiscalReceptor = codigoPostal,
                            regimenFiscal = regimenes?
                                .FirstOrDefault(r => r.IdRegimenFiscal == c.IdRegimenFiscal)?
                                .C_RegimenFiscal,
                            regimenFiscalReceptor = regimenes?
                                .FirstOrDefault(r => r.IdRegimenFiscal == c.IdRegimenFiscal)?
                                .C_RegimenFiscal,
                            fechaRegistro = c.FechaRegistro,
                            pais = paises?
                                .FirstOrDefault(p => p.IdPais == c.IdPais)?
                                .Descripcion,
                            estado = estados?
                                .FirstOrDefault(e => e.IdEstado == c.IdEstado)?
                                .NombreDelEstado,
                            municipio = municipios?
                                .FirstOrDefault(m => m.IdMunicipio == c.IdMunicipio)?
                                .Descripcion
                        };
                    })
                    .ToList();

                return Json(resultado);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = true,
                    mensaje = ex.Message
                });
            }
        }

        #endregion
    }

    public class ClienteApiDto
    {
        public int IdCliente { get; set; }
        public string? NombreCliente { get; set; }
        public string? RfcCliente { get; set; }
        public string? DireccionFiscal { get; set; }
        public int? IdCodigoPostal { get; set; }
        public string? CorreoElectronico { get; set; }
        public int? IdRegimenFiscal { get; set; }
        public int? IdPais { get; set; }
        public int? IdEstado { get; set; }
        public int? IdMunicipio { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    public class CreaClienteApiDto
    {
        public string? NombreCliente { get; set; }
        public string? RfcCliente { get; set; }
        public string? DireccionFiscal { get; set; }
        public int? IdCodigoPostal { get; set; }
        public string? CorreoElectronico { get; set; }
        public int? IdRegimenFiscal { get; set; }
        public int? IdPais { get; set; }
        public int? IdEstado { get; set; }
        public int? IdMunicipio { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public int? IdStatusRegistro { get; set; }
    }
}