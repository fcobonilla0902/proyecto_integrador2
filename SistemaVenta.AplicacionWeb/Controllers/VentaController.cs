using AutoMapper;
using CloudinaryDotNet;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.AplicacionWeb.Utilidades.Response;
using SistemaVenta.BLL.Implementacion;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.Entity;
using SistemaVenta.Entity.Models;
using System.Security.Claims;
using System.Security;
using System.Globalization;
using System.Text;


namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class VentaController : Controller
    {
        private readonly IFacturacionService _facturacionServicio;
        private readonly INegocioService _negocioService;
        private readonly IConverter _converter;
        private readonly IWebHostEnvironment _env;
        private readonly IApiClient _api;

        public VentaController(
            INegocioService negocioService,
            IFacturacionService facturacionServicio,
            IConverter converter,
            IWebHostEnvironment env,
            IApiClient api
        )
        {
            _negocioService = negocioService;
            _facturacionServicio = facturacionServicio;
            _converter = converter;
            _env = env;
            _api = api;
        }

        public IActionResult NuevaVenta()
        {
            return View();
        }

        public IActionResult HistorialVenta()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ListaTipoDocumentoVenta()
        {
            try
            {
                List<TipoDocumentoVentaApiDto>? tiposApi =
                    await _api.GetAsync<List<TipoDocumentoVentaApiDto>>("api/tipodocumentoventa");

                var resultado = tiposApi?.Select(t => new VMTipoDocumentoVenta
                {
                    IdTipoDocumentoVenta = t.IdTipoDocumentoVenta,
                    Descripcion = t.Descripcion
                }).ToList() ?? new List<VMTipoDocumentoVenta>();

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    estado = false,
                    mensaje = $"Error al obtener tipos de documento desde API: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListaMetodoPago()
        {
            try
            {
                List<MetodoPagoApiDto>? metodosPagoApi =
                    await _api.GetAsync<List<MetodoPagoApiDto>>("api/metodopagosat");

                if (metodosPagoApi == null)
                    return StatusCode(StatusCodes.Status200OK, new List<object>());

                var resultado = metodosPagoApi
                    .Where(x => x.IdStatusRegistro == null || x.IdStatusRegistro == 1)
                    .Select(x => new
                    {
                        idMetodoPago = x.IdMetodoPago,
                        descripcion = $"{x.C_MetodoPago} - {x.Descripcion}"
                    })
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListaUsoCFDI()
        {
            try
            {
                List<UsoCFDIApiDto>? usosCFDIApi =
                    await _api.GetAsync<List<UsoCFDIApiDto>>("api/usocfdisat");

                if (usosCFDIApi == null)
                    return StatusCode(StatusCodes.Status200OK, new List<object>());

                var resultado = usosCFDIApi
                    .Where(x => x.IdStatusRegistro == null || x.IdStatusRegistro == 1)
                    .Select(x => new
                    {
                        idUsoCFDI = x.IdUsoCFDI,
                        descripcion = $"{x.C_UsoCFDI} - {x.Descripcion}"
                    })
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListaFormaPago()
        {
            try
            {
                List<FormaPagoApiDto>? formasPagoApi =
                    await _api.GetAsync<List<FormaPagoApiDto>>("api/formapagosat");

                if (formasPagoApi == null)
                    return StatusCode(StatusCodes.Status200OK, new List<object>());

                var resultado = formasPagoApi
                    .Where(x => x.IdStatusRegistro == null || x.IdStatusRegistro == 1)
                    .Select(x => new
                    {
                        idFormaPago = x.IdFormaPago,
                        descripcion = $"{x.C_FormaPago} - {x.Descripcion}"
                    })
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProductos(string busqueda)
        {
            try
            {
                List<ProductoApiDto>? productosApi = await _api.GetAsync<List<ProductoApiDto>>("api/producto");

                if (productosApi == null)
                    return StatusCode(StatusCodes.Status200OK, new List<VMProducto>());

                List<MarcaApiNuevaVentaDto>? marcasApi = await _api.GetAsync<List<MarcaApiNuevaVentaDto>>("api/marca");
                List<CategoriaApiNuevaVentaDto>? categoriasApi = await _api.GetAsync<List<CategoriaApiNuevaVentaDto>>("api/categoria");

                var productosFiltrados = productosApi
                    .Where(p =>
                        p.IdStatusRegistro == 1 &&
                        (p.Stock ?? 0) > 0 &&
                        string.Concat(p.SkuCodigoDeBarras, p.Descripcion)
                            .ToLower()
                            .Contains((busqueda ?? "").ToLower())
                    )
                    .Select(p => new VMProducto
                    {
                        IdProducto = p.IdProducto,
                        CodigoBarra = p.SkuCodigoDeBarras,
                        Descripcion = p.Descripcion,
                        Marca = marcasApi?.FirstOrDefault(m => m.IdMarca == p.IdMarca)?.Descripcion ?? "",
                        NombreCategoria = categoriasApi?.FirstOrDefault(c => c.IdCategoria == p.IdCategoria)?.Descripcion ?? "",
                        Stock = p.Stock,
                        Precio = p.PrecioUnitarioVenta?.ToString("0.00"),
                        UrlImagen = p.UrlImagen ?? "",
                        Descuento = p.Descuento
                    })
                    .ToList();

                return Ok(productosFiltrados);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    estado = false,
                    mensaje = $"Error al obtener productos desde API: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BuscarClientes(string busqueda)
        {
            try
            {
                var clientes =
                    await _api.GetAsync<List<ClienteApiDto>>("api/cliente?skip=0&take=99999")
                    ?? new List<ClienteApiDto>();

                var codigosPostales =
                    await _api.GetAsync<List<CodigoPostalSATApiDto>>("api/codigopostalsat?skip=0&take=99999")
                    ?? new List<CodigoPostalSATApiDto>();

                var regimenes =
                    await _api.GetAsync<List<RegimenFiscalSATApiDto>>("api/regimenfiscalsat")
                    ?? new List<RegimenFiscalSATApiDto>();

                var paises =
                    await _api.GetAsync<List<PaisSATApiDto>>("api/paissat?skip=0&take=99999")
                    ?? new List<PaisSATApiDto>();

                var estados =
                    await _api.GetAsync<List<EstadoSATApiDto>>("api/estadosat?skip=0&take=99999")
                    ?? new List<EstadoSATApiDto>();

                var municipios =
                    await _api.GetAsync<List<MunicipioSATApiDto>>("api/municipiosat?skip=0&take=99999")
                    ?? new List<MunicipioSATApiDto>();

                var textoBusqueda = (busqueda ?? string.Empty).Trim();

                var resultado = clientes
                    .Where(c =>
                        string.IsNullOrWhiteSpace(textoBusqueda) ||
                        (!string.IsNullOrWhiteSpace(c.NombreCliente) &&
                         c.NombreCliente.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(c.RfcCliente) &&
                         c.RfcCliente.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(c.CorreoElectronico) &&
                         c.CorreoElectronico.Contains(textoBusqueda, StringComparison.OrdinalIgnoreCase))
                    )
                    .Take(20)
                    .Select(c =>
                    {
                        string? codigoPostal = codigosPostales
                            .FirstOrDefault(cp => cp.IdCodigoPostal == c.IdCodigoPostal)?
                            .C_CodigoPostal;

                        string? regimenFiscal = regimenes
                            .FirstOrDefault(r => r.IdRegimenFiscal == c.IdRegimenFiscal)?
                            .C_RegimenFiscal;

                        string? pais = paises
                            .FirstOrDefault(p => p.IdPais == c.IdPais)?
                            .Descripcion;

                        string? estado = estados
                            .FirstOrDefault(e => e.IdEstado == c.IdEstado)?
                            .NombreDelEstado;

                        string? municipio = municipios
                            .FirstOrDefault(m => m.IdMunicipio == c.IdMunicipio)?
                            .Descripcion;

                        return new
                        {
                            idCliente = c.IdCliente,
                            nombreCompleto = c.NombreCliente ?? string.Empty,
                            correo = c.CorreoElectronico ?? string.Empty,
                            rfc = c.RfcCliente ?? string.Empty,
                            domicilioFiscal = c.DireccionFiscal ?? string.Empty,
                            domicilioFiscalReceptor = codigoPostal ?? string.Empty,
                            regimenFiscal = regimenFiscal ?? string.Empty,
                            regimenFiscalReceptor = regimenFiscal ?? string.Empty,
                            fechaRegistro = c.FechaRegistro,
                            pais = pais ?? string.Empty,
                            estado = estado ?? string.Empty,
                            municipio = municipio ?? string.Empty
                        };
                    })
                    .ToList();

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    estado = false,
                    mensaje = $"Error al obtener clientes desde API: {ex.Message}"
                });
            }
        }

        private async Task<UsuarioApiDto?> ObtenerUsuarioActualDesdeApi()
        {
            ClaimsPrincipal claimUser = HttpContext.User;

            string? idUsuarioClaim = claimUser.Claims
                .FirstOrDefault(c =>
                    c.Type == "IdUsuarioApi" ||
                    c.Type == ClaimTypes.NameIdentifier)?
                .Value;

            List<UsuarioApiDto>? usuariosApi = await _api.GetAsync<List<UsuarioApiDto>>("api/usuario");

            if (usuariosApi == null || !usuariosApi.Any())
                return null;

            if (!string.IsNullOrWhiteSpace(idUsuarioClaim) && int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                var usuarioPorId = usuariosApi.FirstOrDefault(u => u.IdUsuario == idUsuario);
                if (usuarioPorId != null)
                    return usuarioPorId;
            }

            string? correo = claimUser.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email)?
                .Value;

            if (string.IsNullOrWhiteSpace(correo))
                correo = claimUser.Identity?.Name;

            if (string.IsNullOrWhiteSpace(correo))
                return null;

            return usuariosApi.FirstOrDefault(u =>
                !string.IsNullOrWhiteSpace(u.Correo) &&
                u.Correo.Trim().Equals(correo.Trim(), StringComparison.OrdinalIgnoreCase));
        }


        [HttpPost]
        public async Task<IActionResult> RegistrarVenta([FromBody] VMVenta? modelo)
        {
            GenericResponse<VMVenta> gResponse = new GenericResponse<VMVenta>();

            try
            {
                if (modelo == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se recibió la información de la venta";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                if (modelo.DetalleVenta == null || !modelo.DetalleVenta.Any())
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se recibieron productos en la venta";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                UsuarioApiDto? usuarioApi = await ObtenerUsuarioActualDesdeApi();

                if (usuarioApi == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se pudo obtener el usuario actual desde la API";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                modelo.IdUsuario = usuarioApi.IdUsuario;

                Console.WriteLine("=========== DEBUG VM DETALLE VENTA ===========");
                foreach (var d in modelo.DetalleVenta)
                {
                    Console.WriteLine($"IdProducto: {d.IdProducto}");
                    Console.WriteLine($"DescripcionProducto: {d.DescripcionProducto}");
                    Console.WriteLine($"Cantidad: {d.Cantidad}");
                    Console.WriteLine($"Precio: {d.Precio}");
                    Console.WriteLine($"SubTotal: {d.SubTotal}");
                    Console.WriteLine($"Descuento: {d.Descuento}");
                    Console.WriteLine($"DescuentoDinero: {d.DescuentoDinero}");
                    Console.WriteLine($"Impuesto: {d.Impuesto}");
                    Console.WriteLine($"ImpuestoDinero: {d.ImpuestoDinero}");
                    Console.WriteLine($"Total: {d.Total}");
                    Console.WriteLine("----------------------------------------------");
                }
                Console.WriteLine("=============================================");

                if (modelo.IdTipoDocumentoVenta == 2)
                {
                    List<NumeroCorrelativoApiDto>? correlativos =
                        await _api.GetAsync<List<NumeroCorrelativoApiDto>>("api/numerocorrelativo");

                    NumeroCorrelativoApiDto? correlativo =
                        correlativos?.FirstOrDefault(c => c.Gestion == "venta");

                    string numeroVenta = "000001";

                    if (correlativo != null)
                    {
                        int minimoSeguro = 2199;

                        if ((correlativo.UltimoNumero ?? 0) < minimoSeguro)
                        {
                            Console.WriteLine($"[CORRELATIVO] Ajustando UltimoNumero de {correlativo.UltimoNumero} a {minimoSeguro}");
                            correlativo.UltimoNumero = minimoSeguro;
                        }

                        int nuevoNumero = (correlativo.UltimoNumero ?? 0) + 1;
                        int digitos = correlativo.CantidadDigitos ?? 6;

                        numeroVenta = nuevoNumero.ToString().PadLeft(digitos, '0');

                        correlativo.UltimoNumero = nuevoNumero;
                        correlativo.FechaActualizacion = DateTime.Now;

                        await _api.PutAsync<NumeroCorrelativoApiDto>("api/numerocorrelativo", correlativo);
                    }

                    decimal subTotalGeneral = 0m;
                    decimal descuentoGeneral = 0m;
                    decimal impuestoGeneral = 0m;
                    decimal totalGeneral = 0m;

                    var detallesNormalizados = new List<CreaDetalleVentaApiDto>();

                    foreach (var detalle in modelo.DetalleVenta)
                    {
                        decimal cantidad = detalle.Cantidad;
                        decimal precio = detalle.Precio;

                        decimal subTotalLinea = Math.Round(cantidad * precio, 2, MidpointRounding.AwayFromZero);

                        decimal tasaDescuento = detalle.Descuento;
                        if (tasaDescuento > 1)
                            tasaDescuento = tasaDescuento / 100m;

                        decimal descuentoDinero = detalle.DescuentoDinero > 0
                            ? Math.Round(detalle.DescuentoDinero, 2, MidpointRounding.AwayFromZero)
                            : Math.Round(subTotalLinea * tasaDescuento, 2, MidpointRounding.AwayFromZero);

                        decimal baseImpuesto = subTotalLinea - descuentoDinero;

                        decimal tasaImpuesto = detalle.Impuesto;
                        if (tasaImpuesto > 1)
                            tasaImpuesto = tasaImpuesto / 100m;

                        decimal impuestoDinero = detalle.ImpuestoDinero > 0
                            ? Math.Round(detalle.ImpuestoDinero, 2, MidpointRounding.AwayFromZero)
                            : Math.Round(baseImpuesto * tasaImpuesto, 2, MidpointRounding.AwayFromZero);

                        decimal totalLinea = Math.Round(baseImpuesto + impuestoDinero, 2, MidpointRounding.AwayFromZero);

                        subTotalGeneral += subTotalLinea;
                        descuentoGeneral += descuentoDinero;
                        impuestoGeneral += impuestoDinero;
                        totalGeneral += totalLinea;

                        detallesNormalizados.Add(new CreaDetalleVentaApiDto
                        {
                            IdProducto = detalle.IdProducto,
                            Cantidad = cantidad,
                            Precio = precio,
                            SubTotal = subTotalLinea,
                            Descuento = tasaDescuento,
                            DescuentoDinero = descuentoDinero,
                            Impuesto = tasaImpuesto,
                            ImpuestoDinero = impuestoDinero,
                            Total = totalLinea
                        });
                    }
                    int? idFormaPago = modelo.IdFormaPago;
                    int? idMetodoPago = modelo.IdMetodoPago;
                    int? idUsoCfdi = modelo.IdUsoCFDI;

                    FormaPagoApiDto? formaPagoApiValidacion = null;
                    MetodoPagoApiDto? metodoPagoApiValidacion = null;
                    UsoCFDIApiDto? usoCfdiApiValidacion = null;

                    if (idFormaPago.HasValue && idFormaPago.Value > 0)
                        formaPagoApiValidacion = await _api.GetAsync<FormaPagoApiDto>($"api/formapagosat/{idFormaPago.Value}");

                    if (idMetodoPago.HasValue && idMetodoPago.Value > 0)
                        metodoPagoApiValidacion = await _api.GetAsync<MetodoPagoApiDto>($"api/metodopagosat/{idMetodoPago.Value}");

                    if (idUsoCfdi.HasValue && idUsoCfdi.Value > 0)
                        usoCfdiApiValidacion = await _api.GetAsync<UsoCFDIApiDto>($"api/usocfdisat/{idUsoCfdi.Value}");

                    string claveFormaPagoValidacion = formaPagoApiValidacion?.C_FormaPago?.Trim() ?? "";
                    string claveMetodoPagoValidacion = metodoPagoApiValidacion?.C_MetodoPago?.Trim() ?? "";
                    string claveUsoCfdiValidacion = usoCfdiApiValidacion?.C_UsoCFDI?.Trim() ?? "";

                    Console.WriteLine("=========== DEBUG VALIDACION CFDI PREVIA ===========");
                    Console.WriteLine($"modelo.IdFormaPago: {idFormaPago}");
                    Console.WriteLine($"claveFormaPagoValidacion: {claveFormaPagoValidacion}");
                    Console.WriteLine($"modelo.IdMetodoPago: {idMetodoPago}");
                    Console.WriteLine($"claveMetodoPagoValidacion: {claveMetodoPagoValidacion}");
                    Console.WriteLine($"modelo.IdUsoCFDI: {idUsoCfdi}");
                    Console.WriteLine($"claveUsoCfdiValidacion: {claveUsoCfdiValidacion}");
                    Console.WriteLine("====================================================");

                    // Regla SAT: si es PPD, la forma debe ser 99
                    if (claveMetodoPagoValidacion == "PPD" && claveFormaPagoValidacion != "99")
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "Configuración inválida para facturar: si el método de pago es PPD, la forma de pago debe ser 99 - Por definir.";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }

                    // Regla práctica para factura inmediata: si la forma no es 99, no debe ser PPD
                    if (claveFormaPagoValidacion != "99" && claveMetodoPagoValidacion == "PPD")
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "Configuración inválida para facturar: forma de pago 01/02/03/etc requiere método PUE, no PPD.";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }

                    // CP01 es para complemento de pagos, no para factura de ingreso normal
                    if (claveUsoCfdiValidacion == "CP01")
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "Configuración inválida para facturar: el uso CFDI CP01 corresponde a complemento de pagos, no a una factura de ingreso.";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }
                    var ventaApiDto = new CreaVentaApiDto
                    {
                        IdUsuario = modelo.IdUsuario,
                        NumeroVenta = numeroVenta,
                        IdTipoDocumentoVenta = 1,
                        IdCliente = modelo.IdCliente,
                        IdMetodoPago = modelo.IdMetodoPago,
                        IdUsoCFDI = modelo.IdUsoCFDI,
                        IdFormaPago = modelo.IdFormaPago,
                        SubTotalVenta = Math.Round(subTotalGeneral, 2, MidpointRounding.AwayFromZero),
                        ImpuestoVenta = Math.Round(impuestoGeneral, 2, MidpointRounding.AwayFromZero),
                        TotalVenta = Math.Round(totalGeneral, 2, MidpointRounding.AwayFromZero),
                        FechaRegistroVenta = DateTime.Now
                    };

                    VentaApiDto? ventaCreada = await _api.PostAsync<VentaApiDto>("api/venta", ventaApiDto);

                    if (ventaCreada == null || ventaCreada.IdVenta <= 0)
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "No se pudo registrar la venta/factura en la API";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }

                    foreach (var detalle in detallesNormalizados)
                    {
                        detalle.IdVenta = ventaCreada.IdVenta;
                        await _api.PostAsync<DetalleVentaApiDto>("api/detalleventa", detalle);
                    }

                    if (!modelo.IdCliente.HasValue || modelo.IdCliente.Value <= 0)
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "La factura requiere un cliente válido";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }

                    ClienteApiDto? clienteApi = await _api.GetAsync<ClienteApiDto>($"api/cliente/{modelo.IdCliente.Value}");

                    if (clienteApi == null)
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "Cliente no encontrado en la API";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }

                    Console.WriteLine("========================================");
                    Console.WriteLine($"[DEBUG CLIENTE API] IdCliente: {clienteApi.IdCliente}");
                    Console.WriteLine($"[DEBUG CLIENTE API] Nombre: {clienteApi.NombreCliente}");
                    Console.WriteLine($"[DEBUG CLIENTE API] RFC: {clienteApi.RfcCliente}");
                    Console.WriteLine($"[DEBUG CLIENTE API] IdCodigoPostal: {clienteApi.IdCodigoPostal}");
                    Console.WriteLine($"[DEBUG CLIENTE API] IdRegimenFiscal: {clienteApi.IdRegimenFiscal}");
                    Console.WriteLine("========================================");

                    CodigoPostalSATDto? codigoPostalSat = null;
                    RegimenFiscalSATDto? regimenFiscalSat = null;

                    if (clienteApi.IdCodigoPostal.HasValue && clienteApi.IdCodigoPostal.Value > 0)
                    {
                        codigoPostalSat = await _api.GetAsync<CodigoPostalSATDto>($"api/codigopostalsat/{clienteApi.IdCodigoPostal.Value}");
                    }

                    if (clienteApi.IdRegimenFiscal.HasValue && clienteApi.IdRegimenFiscal.Value > 0)
                    {
                        regimenFiscalSat = await _api.GetAsync<RegimenFiscalSATDto>($"api/regimenfiscalsat/{clienteApi.IdRegimenFiscal.Value}");
                    }

                    Cliente cliente = new Cliente
                    {
                        IdCliente = clienteApi.IdCliente,
                        Nombre = clienteApi.NombreCliente,
                        Correo = clienteApi.CorreoElectronico,
                        Rfc = clienteApi.RfcCliente,
                        DomicilioFiscalReceptor = codigoPostalSat?.C_CodigoPostal,
                        RegimenFiscalReceptor = regimenFiscalSat?.C_RegimenFiscal,
                        EsActivo = clienteApi.IdStatusRegistro == 1,
                        FechaRegistro = clienteApi.FechaRegistro ?? DateTime.Now
                    };

                    Console.WriteLine("========================================");
                    Console.WriteLine($"[DEBUG CLIENTE MAPEADO] Nombre: {cliente.Nombre}");
                    Console.WriteLine($"[DEBUG CLIENTE MAPEADO] RFC: {cliente.Rfc}");
                    Console.WriteLine($"[DEBUG CLIENTE MAPEADO] DomicilioFiscalReceptor: {cliente.DomicilioFiscalReceptor}");
                    Console.WriteLine($"[DEBUG CLIENTE MAPEADO] RegimenFiscalReceptor: {cliente.RegimenFiscalReceptor}");
                    Console.WriteLine("========================================");

                    if (!_facturacionServicio.ValidarDatosFiscalesCliente(cliente))
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "El cliente no tiene datos fiscales completos";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }

                    List<ProductoApiDto> productosApiTimbrado =
                        await _api.GetAsync<List<ProductoApiDto>>("api/producto") ?? new List<ProductoApiDto>();

                    Console.WriteLine($"[SAT] productosApiTimbrado count: {productosApiTimbrado.Count}");

                    var detallesParaTimbrar = new List<DetalleVenta>();

                    foreach (var d in modelo.DetalleVenta)
                    {
                        var prodApi = productosApiTimbrado.FirstOrDefault(p => p.IdProducto == d.IdProducto);

                        string? claveProdServ = null;
                        string? claveUnidad = null;

                        if (prodApi != null)
                        {
                            if (prodApi.IdClaveProdServSAT.HasValue)
                            {
                                var catProd = await _api.GetAsync<ClaveProdServSATDto>($"api/claveprodservsat/{prodApi.IdClaveProdServSAT.Value}");
                                claveProdServ = catProd?.C_ClaveProdServ;
                            }

                            if (prodApi.IdClaveUnidadSAT.HasValue)
                            {
                                var catUnidad = await _api.GetAsync<ClaveUnidadSATDto>($"api/claveunidadsat/{prodApi.IdClaveUnidadSAT.Value}");
                                claveUnidad = catUnidad?.C_ClaveUnidad;
                            }
                        }

                        Console.WriteLine($"[SAT] Producto: {d.IdProducto}");
                        Console.WriteLine($"[SAT] Descripción: {d.DescripcionProducto}");
                        Console.WriteLine($"[SAT] IdClaveProdServSAT en productoApi: {prodApi?.IdClaveProdServSAT}");
                        Console.WriteLine($"[SAT] IdClaveUnidadSAT en productoApi: {prodApi?.IdClaveUnidadSAT}");
                        Console.WriteLine($"[SAT] ClaveProdServ resuelta: {claveProdServ}");
                        Console.WriteLine($"[SAT] ClaveUnidad resuelta: {claveUnidad}");

                        decimal cantidad = d.Cantidad;
                        decimal precioUnitario = d.Precio;
                        decimal importeSinDescuento = Math.Round(cantidad * precioUnitario, 2, MidpointRounding.AwayFromZero);

                        decimal tasaDescuento = d.Descuento;
                        if (tasaDescuento > 1)
                            tasaDescuento = tasaDescuento / 100m;

                        decimal descuentoDinero = d.DescuentoDinero > 0
                            ? Math.Round(d.DescuentoDinero, 2, MidpointRounding.AwayFromZero)
                            : Math.Round(importeSinDescuento * tasaDescuento, 2, MidpointRounding.AwayFromZero);

                        decimal tasaImpuesto = d.Impuesto;
                        if (tasaImpuesto > 1)
                            tasaImpuesto = tasaImpuesto / 100m;

                        decimal baseImpuesto = Math.Round(importeSinDescuento - descuentoDinero, 2, MidpointRounding.AwayFromZero);

                        decimal impuestoDinero = d.ImpuestoDinero > 0
                            ? Math.Round(d.ImpuestoDinero, 2, MidpointRounding.AwayFromZero)
                            : Math.Round(baseImpuesto * tasaImpuesto, 2, MidpointRounding.AwayFromZero);

                        detallesParaTimbrar.Add(new DetalleVenta
                        {
                            IdProducto = d.IdProducto,
                            DescripcionProducto = d.DescripcionProducto,
                            Cantidad = d.Cantidad,
                            Precio = d.Precio,
                            SubTotal = importeSinDescuento,
                            Descuento = tasaDescuento,
                            DescuentoDinero = descuentoDinero,
                            Impuesto = tasaImpuesto,
                            ImpuestoDinero = impuestoDinero,
                            Total = Math.Round(baseImpuesto + impuestoDinero, 2, MidpointRounding.AwayFromZero),
                            ClaveProdServSat = claveProdServ,
                            ClaveUnidadSat = claveUnidad,
                            ObjetoImpuesto = "02",
                            TipoImpuesto = "002",
                            FactorImpuesto = "Tasa",
                            ValorImpuesto = tasaImpuesto > 0 ? tasaImpuesto : 0.160000m,
                            Unidad = "PZA"
                        });
                    }

                    FormaPagoApiDto? formaPagoApi = null;
                    MetodoPagoApiDto? metodoPagoApi = null;
                    UsoCFDIApiDto? usoCfdiApi = null;

                    if (ventaCreada.IdFormaPago.HasValue && ventaCreada.IdFormaPago.Value > 0)
                        formaPagoApi = await _api.GetAsync<FormaPagoApiDto>($"api/formapagosat/{ventaCreada.IdFormaPago.Value}");

                    if (ventaCreada.IdMetodoPago.HasValue && ventaCreada.IdMetodoPago.Value > 0)
                        metodoPagoApi = await _api.GetAsync<MetodoPagoApiDto>($"api/metodopagosat/{ventaCreada.IdMetodoPago.Value}");

                    if (ventaCreada.IdUsoCFDI.HasValue && ventaCreada.IdUsoCFDI.Value > 0)
                        usoCfdiApi = await _api.GetAsync<UsoCFDIApiDto>($"api/usocfdisat/{ventaCreada.IdUsoCFDI.Value}");

                    Console.WriteLine("=========== DEBUG CFDI VENTA ===========");
                    Console.WriteLine($"ventaCreada.IdFormaPago: {ventaCreada.IdFormaPago}");
                    Console.WriteLine($"formaPagoApi: {(formaPagoApi == null ? "NULL" : $"{formaPagoApi.C_FormaPago} - {formaPagoApi.Descripcion}")}");
                    Console.WriteLine($"ventaCreada.IdMetodoPago: {ventaCreada.IdMetodoPago}");
                    Console.WriteLine($"metodoPagoApi: {(metodoPagoApi == null ? "NULL" : $"{metodoPagoApi.C_MetodoPago} - {metodoPagoApi.Descripcion}")}");
                    Console.WriteLine($"ventaCreada.IdUsoCFDI: {ventaCreada.IdUsoCFDI}");
                    Console.WriteLine($"usoCfdiApi: {(usoCfdiApi == null ? "NULL" : $"{usoCfdiApi.C_UsoCFDI} - {usoCfdiApi.Descripcion}")}");
                    Console.WriteLine("========================================");

                    var ventaParaTimbrar = new Venta
                    {
                        IdVenta = ventaCreada.IdVenta,
                        NumeroVenta = ventaCreada.NumeroVenta,
                        IdTipoDocumentoVenta = ventaCreada.IdTipoDocumentoVenta,
                        IdCliente = ventaCreada.IdCliente,

                        IdMetodoPago = ventaCreada.IdMetodoPago,
                        IdUsoCFDI = ventaCreada.IdUsoCFDI,
                        IdFormaPago = ventaCreada.IdFormaPago,

                        ClaveFormaPago = !string.IsNullOrWhiteSpace(formaPagoApi?.C_FormaPago)
                            ? formaPagoApi.C_FormaPago.Trim()
                            : "01",

                        FormaPago = formaPagoApi != null
                            ? $"{formaPagoApi.C_FormaPago?.Trim()} - {formaPagoApi.Descripcion?.Trim()}"
                            : "01 - Efectivo",

                        ClaveMetodoPago = !string.IsNullOrWhiteSpace(metodoPagoApi?.C_MetodoPago)
                            ? metodoPagoApi.C_MetodoPago.Trim()
                            : "PUE",

                        MetodoPago = metodoPagoApi != null
                            ? $"{metodoPagoApi.C_MetodoPago?.Trim()} - {metodoPagoApi.Descripcion?.Trim()}"
                            : "PUE - Pago en una sola exhibición",

                        ClaveUsoCFDI = !string.IsNullOrWhiteSpace(usoCfdiApi?.C_UsoCFDI)
                            ? usoCfdiApi.C_UsoCFDI.Trim()
                            : "S01",

                        UsoCFDI = usoCfdiApi != null
                            ? $"{usoCfdiApi.C_UsoCFDI?.Trim()} - {usoCfdiApi.Descripcion?.Trim()}"
                            : "S01 - Sin efectos fiscales",

                        SubTotal = ventaCreada.SubTotalVenta,
                        ImpuestoTotal = ventaCreada.ImpuestoVenta,
                        Total = ventaCreada.TotalVenta,
                        FechaRegistro = ventaCreada.FechaRegistroVenta,
                        NombreCliente = cliente.Nombre,
                        DocumentoCliente = cliente.Rfc,

                        DetalleVenta = detallesParaTimbrar
                    };

                    Console.WriteLine("=========== DEBUG DETALLE PARA TIMBRAR ===========");
                    foreach (var d in ventaParaTimbrar.DetalleVenta)
                    {
                        Console.WriteLine($"IdProducto: {d.IdProducto}");
                        Console.WriteLine($"DescripcionProducto: {d.DescripcionProducto}");
                        Console.WriteLine($"Cantidad: {d.Cantidad}");
                        Console.WriteLine($"Precio: {d.Precio}");
                        Console.WriteLine($"SubTotal: {d.SubTotal}");
                        Console.WriteLine($"DescuentoDinero: {d.DescuentoDinero}");
                        Console.WriteLine($"ImpuestoDinero: {d.ImpuestoDinero}");
                        Console.WriteLine($"Total: {d.Total}");
                        Console.WriteLine($"ClaveProdServSat: {d.ClaveProdServSat}");
                        Console.WriteLine($"ClaveUnidadSat: {d.ClaveUnidadSat}");
                        Console.WriteLine($"ObjetoImpuesto: {d.ObjetoImpuesto}");
                        Console.WriteLine($"TipoImpuesto: {d.TipoImpuesto}");
                        Console.WriteLine($"FactorImpuesto: {d.FactorImpuesto}");
                        Console.WriteLine($"ValorImpuesto: {d.ValorImpuesto}");
                        Console.WriteLine("----------------------------------------------");
                    }
                    Console.WriteLine("=========== DEBUG DATOS CFDI TIMBRAR ===========");
                    Console.WriteLine($"ClaveFormaPago: {ventaParaTimbrar.ClaveFormaPago}");
                    Console.WriteLine($"FormaPago: {ventaParaTimbrar.FormaPago}");
                    Console.WriteLine($"ClaveMetodoPago: {ventaParaTimbrar.ClaveMetodoPago}");
                    Console.WriteLine($"MetodoPago: {ventaParaTimbrar.MetodoPago}");
                    Console.WriteLine($"ClaveUsoCFDI: {ventaParaTimbrar.ClaveUsoCFDI}");
                    Console.WriteLine($"UsoCFDI: {ventaParaTimbrar.UsoCFDI}");
                    Console.WriteLine("================================================");

                    await CompletarCatalogosFiscales(detallesParaTimbrar, productosApiTimbrado, ventaParaTimbrar);
                    Negocio negocio = await _negocioService.Obtener();

                    if (negocio == null)
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "No se encontró la configuración del negocio";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }

                    string rutaFacturas = Path.Combine(_env.WebRootPath, "Facturas");

                    var resultado = await _facturacionServicio.TimbrarVenta(
                        ventaParaTimbrar,
                        cliente,
                        negocio,
                        rutaFacturas
                    );

                    Console.WriteLine($"[REGISTRAR VENTA] exito: {resultado.exito}");
                    Console.WriteLine($"[REGISTRAR VENTA] mensaje: {resultado.mensaje}");
                    Console.WriteLine($"[REGISTRAR VENTA] rutaPDF devuelta: {resultado.rutaPDF}");
                    Console.WriteLine($"[REGISTRAR VENTA] rutaXML devuelta: {resultado.rutaXML}");
                    Console.WriteLine($"[REGISTRAR VENTA] nombre PDF: {Path.GetFileName(resultado.rutaPDF ?? string.Empty)}");
                    Console.WriteLine($"[REGISTRAR VENTA] nombre XML: {Path.GetFileName(resultado.rutaXML ?? string.Empty)}");

                    if (!resultado.exito || string.IsNullOrWhiteSpace(resultado.rutaPDF) || string.IsNullOrWhiteSpace(resultado.rutaXML))
                    {
                        gResponse.Estado = false;
                        gResponse.Objeto = new VMVenta { IdVenta = ventaCreada.IdVenta, NumeroVenta = ventaCreada.NumeroVenta };
                        gResponse.Mensaje = $"Venta {ventaCreada.NumeroVenta} registrada y pendiente de factura. Reintente desde el historial de esa venta. Detalle: {resultado.mensaje}";
                        return StatusCode(StatusCodes.Status200OK, gResponse);
                    }

                    ventaCreada.Uuid = resultado.uuid;
                    ventaCreada.FechaTimbrado = DateTime.Now;
                    ventaCreada.IdTipoDocumentoVenta = 2;

                    await ConfirmarFacturaApi(ventaCreada);

                    modelo.NumeroVenta = ventaCreada.NumeroVenta;
                    modelo.IdVenta = ventaCreada.IdVenta;
                    modelo.SubTotal = subTotalGeneral.ToString("0.00");
                    modelo.DescuentoTotal = descuentoGeneral.ToString("0.00");
                    modelo.ImpuestoTotal = impuestoGeneral.ToString("0.00");
                    modelo.Total = totalGeneral.ToString("0.00");
                    modelo.UUID = resultado.uuid;
                    modelo.FechaTimbrado = ventaCreada.FechaTimbrado?.ToString("dd/MM/yyyy HH:mm:ss");
                    modelo.FormaPago = ventaParaTimbrar.FormaPago;
                    modelo.ClaveFormaPago = ventaParaTimbrar.ClaveFormaPago;
                    modelo.MetodoPago = ventaParaTimbrar.MetodoPago;
                    modelo.ClaveMetodoPago = ventaParaTimbrar.ClaveMetodoPago;
                    modelo.UsoCFDI = ventaParaTimbrar.UsoCFDI;
                    modelo.ClaveUsoCFDI = ventaParaTimbrar.ClaveUsoCFDI;

                    gResponse.Estado = true;
                    gResponse.Objeto = modelo;
                    gResponse.Mensaje = $"Factura timbrada exitosamente. UUID: {resultado.uuid}";
                }
                else
                {
                    List<NumeroCorrelativoApiDto>? correlativos =
                        await _api.GetAsync<List<NumeroCorrelativoApiDto>>("api/numerocorrelativo");

                    NumeroCorrelativoApiDto? correlativo =
                        correlativos?.FirstOrDefault(c => c.Gestion == "venta");

                    string numeroVenta = "000001";

                    if (correlativo != null)
                    {
                        int minimoSeguro = 2199;

                        if ((correlativo.UltimoNumero ?? 0) < minimoSeguro)
                        {
                            Console.WriteLine($"[CORRELATIVO] Ajustando UltimoNumero de {correlativo.UltimoNumero} a {minimoSeguro}");
                            correlativo.UltimoNumero = minimoSeguro;
                        }

                        int nuevoNumero = (correlativo.UltimoNumero ?? 0) + 1;
                        int digitos = correlativo.CantidadDigitos ?? 6;
                        numeroVenta = nuevoNumero.ToString().PadLeft(digitos, '0');

                        correlativo.UltimoNumero = nuevoNumero;
                        correlativo.FechaActualizacion = DateTime.Now;

                        await _api.PutAsync<NumeroCorrelativoApiDto>("api/numerocorrelativo", correlativo);
                    }

                    decimal subTotalGeneral = 0m;
                    decimal descuentoGeneral = 0m;
                    decimal impuestoGeneral = 0m;
                    decimal totalGeneral = 0m;

                    var detallesNormalizados = new List<CreaDetalleVentaApiDto>();

                    foreach (var detalle in modelo.DetalleVenta)
                    {
                        decimal cantidad = detalle.Cantidad;
                        decimal precio = detalle.Precio;

                        decimal subTotalLinea = Math.Round(cantidad * precio, 2, MidpointRounding.AwayFromZero);

                        decimal tasaDescuento = detalle.Descuento;
                        if (tasaDescuento > 1)
                            tasaDescuento = tasaDescuento / 100m;

                        decimal descuentoDinero = detalle.DescuentoDinero > 0
                            ? Math.Round(detalle.DescuentoDinero, 2, MidpointRounding.AwayFromZero)
                            : Math.Round(subTotalLinea * tasaDescuento, 2, MidpointRounding.AwayFromZero);

                        decimal baseImpuesto = subTotalLinea - descuentoDinero;

                        decimal tasaImpuesto = detalle.Impuesto;
                        if (tasaImpuesto > 1)
                            tasaImpuesto = tasaImpuesto / 100m;

                        decimal impuestoDinero = detalle.ImpuestoDinero > 0
                            ? Math.Round(detalle.ImpuestoDinero, 2, MidpointRounding.AwayFromZero)
                            : Math.Round(baseImpuesto * tasaImpuesto, 2, MidpointRounding.AwayFromZero);

                        decimal totalLinea = Math.Round(baseImpuesto + impuestoDinero, 2, MidpointRounding.AwayFromZero);

                        subTotalGeneral += subTotalLinea;
                        descuentoGeneral += descuentoDinero;
                        impuestoGeneral += impuestoDinero;
                        totalGeneral += totalLinea;

                        detallesNormalizados.Add(new CreaDetalleVentaApiDto
                        {
                            IdProducto = detalle.IdProducto,
                            Cantidad = cantidad,
                            Precio = precio,
                            SubTotal = subTotalLinea,
                            Descuento = tasaDescuento,
                            DescuentoDinero = descuentoDinero,
                            Impuesto = tasaImpuesto,
                            ImpuestoDinero = impuestoDinero,
                            Total = totalLinea
                        });
                    }

                    var ventaApiDto = new CreaVentaApiDto
                    {
                        IdUsuario = modelo.IdUsuario,
                        NumeroVenta = numeroVenta,
                        IdTipoDocumentoVenta = modelo.IdTipoDocumentoVenta,
                        IdCliente = modelo.IdCliente,
                        IdMetodoPago = modelo.IdMetodoPago,
                        IdUsoCFDI = modelo.IdUsoCFDI,
                        IdFormaPago = modelo.IdFormaPago,
                        SubTotalVenta = Math.Round(subTotalGeneral, 2, MidpointRounding.AwayFromZero),
                        ImpuestoVenta = Math.Round(impuestoGeneral, 2, MidpointRounding.AwayFromZero),
                        TotalVenta = Math.Round(totalGeneral, 2, MidpointRounding.AwayFromZero),
                        FechaRegistroVenta = DateTime.Now
                    };

                    VentaApiDto? ventaCreada = await _api.PostAsync<VentaApiDto>("api/venta", ventaApiDto);

                    if (ventaCreada != null && ventaCreada.IdVenta > 0)
                    {
                        foreach (var detalle in detallesNormalizados)
                        {
                            detalle.IdVenta = ventaCreada.IdVenta;
                            await _api.PostAsync<DetalleVentaApiDto>("api/detalleventa", detalle);
                        }

                        modelo.NumeroVenta = ventaCreada.NumeroVenta;
                        modelo.IdVenta = ventaCreada.IdVenta;
                        modelo.SubTotal = subTotalGeneral.ToString("0.00");
                        modelo.DescuentoTotal = descuentoGeneral.ToString("0.00");
                        modelo.ImpuestoTotal = impuestoGeneral.ToString("0.00");
                        modelo.Total = totalGeneral.ToString("0.00");

                        gResponse.Estado = true;
                        gResponse.Objeto = modelo;
                    }
                    else
                    {
                        gResponse.Estado = false;
                        gResponse.Mensaje = "No se pudo registrar la venta en la API";
                    }
                }
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerClientePorId(int idCliente)
        {
            try
            {
                // Traer cliente por id desde tu API
                // Si tu API tiene endpoint diferente (por id: api/cliente/{id}),
                // ajusta la URL.
                ClienteApiDto? cliente = await _api.GetAsync<ClienteApiDto>($"api/cliente/{idCliente}");

                if (cliente == null)
                    return NotFound(new { mensaje = "Cliente no encontrado" });

                PaisApiDto? pais = null;
                EstadoApiDto? estado = null;
                MunicipioApiDto? municipio = null;

                if (cliente.IdPais.HasValue && cliente.IdPais.Value > 0)
                {
                    pais = await _api.GetAsync<PaisApiDto>($"api/paissat/{cliente.IdPais.Value}");
                }

                if (cliente.IdEstado.HasValue && cliente.IdEstado.Value > 0)
                {
                    estado = await _api.GetAsync<EstadoApiDto>($"api/estadosat/{cliente.IdEstado.Value}");
                }

                if (cliente.IdMunicipio.HasValue && cliente.IdMunicipio.Value > 0)
                {
                    municipio = await _api.GetAsync<MunicipioApiDto>($"api/municipiosat/{cliente.IdMunicipio.Value}");
                }

                // Objeto que se usará para autollenar los inputs
                return Json(new
                {
                    idCliente = cliente.IdCliente,
                    nombreCliente = cliente.NombreCliente,
                    correoElectronico = cliente.CorreoElectronico,
                    rfcCliente = cliente.RfcCliente,
                    direccionFiscal = cliente.DireccionFiscal,
                    idRegimenFiscal = cliente.IdRegimenFiscal,
                    fechaRegistro = cliente.FechaRegistro,

                    pais = pais?.Descripcion ?? "",
                    estado = estado?.NombreDelEstado ?? "",
                    municipio = municipio?.Descripcion ?? ""
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Historial(string numeroVenta, string fechaInicio, string fechaFin)
        {
            try
            {
                List<VentaApiDto>? ventasApi = await _api.GetAsync<List<VentaApiDto>>("api/venta");
                List<DetalleVentaApiDto>? detallesApi = await _api.GetAsync<List<DetalleVentaApiDto>>("api/detalleventa");
                List<TipoDocumentoVentaApiDto>? tiposDocumentoApi = await _api.GetAsync<List<TipoDocumentoVentaApiDto>>("api/tipodocumentoventa");
                List<ClienteApiDto>? clientesApi = await _api.GetAsync<List<ClienteApiDto>>("api/cliente");
                List<ProductoApiDto>? productosApi = await _api.GetAsync<List<ProductoApiDto>>("api/producto");
                List<UsuarioApiDto>? usuariosApi = await _api.GetAsync<List<UsuarioApiDto>>("api/usuario");
                List<FormaPagoApiDto>? formasPagoApi = await _api.GetAsync<List<FormaPagoApiDto>>("api/formapagosat");
                List<MetodoPagoApiDto>? metodosPagoApi = await _api.GetAsync<List<MetodoPagoApiDto>>("api/metodopagosat");

                ventasApi ??= new List<VentaApiDto>();
                detallesApi ??= new List<DetalleVentaApiDto>();
                tiposDocumentoApi ??= new List<TipoDocumentoVentaApiDto>();
                clientesApi ??= new List<ClienteApiDto>();
                productosApi ??= new List<ProductoApiDto>();
                usuariosApi ??= new List<UsuarioApiDto>();
                formasPagoApi ??= new List<FormaPagoApiDto>();
                metodosPagoApi ??= new List<MetodoPagoApiDto>();

                DateTime? fechaInicioDate = null;
                DateTime? fechaFinDate = null;

                if (!string.IsNullOrWhiteSpace(fechaInicio))
                    fechaInicioDate = DateTime.ParseExact(fechaInicio, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

                if (!string.IsNullOrWhiteSpace(fechaFin))
                    fechaFinDate = DateTime.ParseExact(fechaFin, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

                IEnumerable<VentaApiDto> query = ventasApi;

                if (!string.IsNullOrWhiteSpace(numeroVenta))
                {
                    query = query.Where(v =>
                        !string.IsNullOrWhiteSpace(v.NumeroVenta) &&
                        v.NumeroVenta.Trim().Equals(numeroVenta.Trim(), StringComparison.OrdinalIgnoreCase));
                }
                else if (fechaInicioDate.HasValue && fechaFinDate.HasValue)
                {
                    query = query.Where(v =>
                        v.FechaRegistroVenta.HasValue &&
                        v.FechaRegistroVenta.Value.Date >= fechaInicioDate.Value.Date &&
                        v.FechaRegistroVenta.Value.Date <= fechaFinDate.Value.Date);
                }

                List<VMVenta> vmHistorialVenta = query
                    .OrderByDescending(v => v.FechaRegistroVenta)
                    .Select(v =>
                    {
                        var detallesVenta = detallesApi
                            .Where(d => d.IdVenta == v.IdVenta)
                            .ToList();

                        var formaPago = formasPagoApi.FirstOrDefault(f => f.IdFormaPago == v.IdFormaPago);
                        var metodoPago = metodosPagoApi.FirstOrDefault(m => m.IdMetodoPago == v.IdMetodoPago);

                        return new VMVenta
                        {
                            IdVenta = v.IdVenta,
                            NumeroVenta = v.NumeroVenta,
                            IdTipoDocumentoVenta = v.IdTipoDocumentoVenta,
                            TipoDocumentoVenta = tiposDocumentoApi
                                .FirstOrDefault(t => t.IdTipoDocumentoVenta == v.IdTipoDocumentoVenta)?.Descripcion ?? "",
                            IdUsuario = v.IdUsuario,
                            Usuario = usuariosApi
                                .FirstOrDefault(u => u.IdUsuario == v.IdUsuario)?.Nombre ?? "",
                            IdCliente = v.IdCliente,
                            NombreCliente = clientesApi
                                .FirstOrDefault(c => c.IdCliente == v.IdCliente)?.NombreCliente ?? "",

                            SubTotal = (v.SubTotalVenta ?? 0).ToString("0.00"),
                            DescuentoTotal = detallesVenta.Sum(d => d.DescuentoDinero ?? 0).ToString("0.00"),
                            ImpuestoTotal = detallesVenta.Sum(d => d.ImpuestoDinero ?? 0).ToString("0.00"),
                            Total = (v.TotalVenta ?? 0).ToString("0.00"),
                            FechaRegistro = v.FechaRegistroVenta?.ToString("dd/MM/yyyy"),

                            IdFormaPago = v.IdFormaPago,
                            ClaveFormaPago = formaPago?.C_FormaPago ?? "",
                            FormaPago = formaPago != null
                                ? $"{formaPago.C_FormaPago} - {formaPago.Descripcion}"
                                : "",

                            IdMetodoPago = v.IdMetodoPago,
                            ClaveMetodoPago = metodoPago?.C_MetodoPago ?? "",
                            MetodoPago = metodoPago != null
                                ? $"{metodoPago.C_MetodoPago} - {metodoPago.Descripcion}"
                                : "",

                            UUID = v.Uuid,
                            FechaTimbrado = v.FechaTimbrado?.ToString("dd/MM/yyyy HH:mm:ss"),

                            DetalleVenta = detallesVenta
                                .Select(d => new VMDetalleVenta
                                {
                                    IdProducto = d.IdProducto ?? 0,
                                    DescripcionProducto = productosApi
                                        .FirstOrDefault(p => p.IdProducto == d.IdProducto)?.Descripcion ?? "",
                                    Cantidad = d.Cantidad.HasValue ? Convert.ToInt32(d.Cantidad.Value) : 0,
                                    Precio = d.Precio ?? 0,
                                    SubTotal = d.SubTotal ?? ((d.Cantidad ?? 0) * (d.Precio ?? 0)),
                                    Descuento = d.Descuento ?? 0,
                                    DescuentoDinero = d.DescuentoDinero ?? 0,

                                    Impuesto = d.Impuesto ?? 0,
                                    ImpuestoDinero = d.ImpuestoDinero ?? 0,

                                    Total = d.Total ?? 0
                                })
                                .ToList()
                        };
                    })
                    .ToList();

                return StatusCode(StatusCodes.Status200OK, vmHistorialVenta);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    mensaje = "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose."
                });
            }
        }

        public IActionResult MostrarPDFVenta(string numeroVenta)
        {
            string urlPlantillaVista = $"{this.Request.Scheme}://{this.Request.Host}/Plantilla/PDFVenta?numeroVenta={numeroVenta}";

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings()
                {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait,
                },
                Objects = {
                    new ObjectSettings()
                    {
                        Page = urlPlantillaVista
                    }
                }
            };

            var archivoPDF = _converter.Convert(pdf);

            return File(archivoPDF, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DescargarFacturaPDF(string numeroVenta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(numeroVenta))
                {
                    return BadRequest("Debe proporcionar el número de venta");
                }

                string carpetaFacturas = Path.Combine(_env.WebRootPath, "Facturas");

                Console.WriteLine($"[DESCARGAR PDF] numeroVenta: {numeroVenta}");
                Console.WriteLine($"[DESCARGAR PDF] WebRootPath: {_env.WebRootPath}");
                Console.WriteLine($"[DESCARGAR PDF] carpetaFacturas: {carpetaFacturas}");

                if (!Directory.Exists(carpetaFacturas))
                {
                    Console.WriteLine("[DESCARGAR PDF] La carpeta de facturas no existe");
                    return NotFound("La carpeta de facturas no existe");
                }

                string patron = $"Factura_{numeroVenta}_*.pdf";

                var archivosPdf = new DirectoryInfo(carpetaFacturas)
                    .EnumerateFiles("*.pdf", SearchOption.TopDirectoryOnly)
                    .Where(f => f.Name.StartsWith($"Factura_{numeroVenta}_", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                foreach (var archivo in archivosPdf)
                {
                    Console.WriteLine($"[DESCARGAR PDF] Archivo candidato: {archivo.FullName}");
                }

                string? archivoPdf = archivosPdf
                    .Select(f => f.FullName)
                    .FirstOrDefault();

                Console.WriteLine($"[DESCARGAR PDF] archivo seleccionado: {archivoPdf}");

                if (string.IsNullOrWhiteSpace(archivoPdf) || !System.IO.File.Exists(archivoPdf))
                {
                    Console.WriteLine($"[DESCARGAR PDF] No se encontró archivo para el patrón: {patron}");
                    return NotFound($"Factura no encontrada para la venta {numeroVenta}");
                }

                byte[] pdfBytes = await System.IO.File.ReadAllBytesAsync(archivoPdf);

                return File(pdfBytes, "application/pdf", $"Factura_{numeroVenta}.pdf");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DESCARGAR PDF] ERROR: {ex}");
                return StatusCode(500, $"Error al descargar el PDF: {ex.Message}");
            }
        }

        [HttpGet]
public async Task<IActionResult> DescargarFacturaXML(string numeroVenta)
{
    try
    {
        if (string.IsNullOrWhiteSpace(numeroVenta))
        {
            return BadRequest("Debe proporcionar el número de venta");
        }

        string carpetaFacturas = Path.Combine(_env.WebRootPath, "Facturas");

        Console.WriteLine($"[DESCARGAR XML] numeroVenta: {numeroVenta}");
        Console.WriteLine($"[DESCARGAR XML] WebRootPath: {_env.WebRootPath}");
        Console.WriteLine($"[DESCARGAR XML] carpetaFacturas: {carpetaFacturas}");

        if (!Directory.Exists(carpetaFacturas))
        {
            Console.WriteLine("[DESCARGAR XML] La carpeta de facturas no existe");
            return NotFound("La carpeta de facturas no existe");
        }

        string patron = $"Factura_{numeroVenta}_*.xml";

        var archivosXml = new DirectoryInfo(carpetaFacturas)
            .EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly)
            .Where(f => f.Name.StartsWith($"Factura_{numeroVenta}_", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();

        foreach (var archivo in archivosXml)
        {
            Console.WriteLine($"[DESCARGAR XML] Archivo candidato: {archivo.FullName}");
        }

        string? archivoXml = archivosXml
            .Select(f => f.FullName)
            .FirstOrDefault();

        Console.WriteLine($"[DESCARGAR XML] archivo seleccionado: {archivoXml}");

        if (string.IsNullOrWhiteSpace(archivoXml) || !System.IO.File.Exists(archivoXml))
        {
            Console.WriteLine($"[DESCARGAR XML] No se encontró archivo para el patrón: {patron}");
            return NotFound($"Factura no encontrada para la venta {numeroVenta}");
        }

        byte[] xmlBytes = await System.IO.File.ReadAllBytesAsync(archivoXml);

        return File(xmlBytes, "application/xml", $"Factura_{numeroVenta}.xml");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DESCARGAR XML] ERROR: {ex}");
        return StatusCode(500, $"Error al descargar el XML: {ex.Message}");
    }
}

        [HttpPost]
        public async Task<IActionResult> SolicitarFactura([FromBody] SolicitudFactura solicitud)
        {
            GenericResponse<object> gResponse = new GenericResponse<object>();

            try
            {
                if (solicitud == null || string.IsNullOrWhiteSpace(solicitud.NumeroVenta))
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "Solicitud inválida";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                var ventasApi = await _api.GetAsync<List<VentaApiDto>>("api/venta") ?? new List<VentaApiDto>();
                var detallesApi = await _api.GetAsync<List<DetalleVentaApiDto>>("api/detalleventa") ?? new List<DetalleVentaApiDto>();
                var productosApi = await _api.GetAsync<List<ProductoApiDto>>("api/producto") ?? new List<ProductoApiDto>();

                var ventaApi = ventasApi.FirstOrDefault(v =>
                    !string.IsNullOrWhiteSpace(v.NumeroVenta) &&
                    v.NumeroVenta.Trim().Equals(solicitud.NumeroVenta.Trim(), StringComparison.OrdinalIgnoreCase));

                if (ventaApi == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "Venta no encontrada";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                if (solicitud.IdVenta > 0 && solicitud.IdVenta != ventaApi.IdVenta)
                    throw new InvalidOperationException("El ID y el número de venta no corresponden.");

                if (ventaApi.IdTipoDocumentoVenta is not (1 or 2))
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "Esta venta ya es una factura";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                if (!string.IsNullOrWhiteSpace(ventaApi.Uuid))
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "Esta venta ya fue timbrada anteriormente";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                if (!ventaApi.IdCliente.HasValue || ventaApi.IdCliente.Value <= 0)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "El ticket no tiene un cliente asignado";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                var detalleVentaApi = detallesApi
                    .Where(d => d.IdVenta == ventaApi.IdVenta)
                    .ToList();

                if (!detalleVentaApi.Any())
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "La venta no tiene detalles";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                ClienteApiDto? clienteApi = await _api.GetAsync<ClienteApiDto>($"api/cliente/{ventaApi.IdCliente.Value}");

                if (clienteApi == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "Cliente no encontrado";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                CodigoPostalSATDto? codigoPostalSat = null;
                RegimenFiscalSATDto? regimenFiscalSat = null;

                if (clienteApi.IdCodigoPostal.HasValue && clienteApi.IdCodigoPostal.Value > 0)
                    codigoPostalSat = await _api.GetAsync<CodigoPostalSATDto>($"api/codigopostalsat/{clienteApi.IdCodigoPostal.Value}");

                if (clienteApi.IdRegimenFiscal.HasValue && clienteApi.IdRegimenFiscal.Value > 0)
                    regimenFiscalSat = await _api.GetAsync<RegimenFiscalSATDto>($"api/regimenfiscalsat/{clienteApi.IdRegimenFiscal.Value}");

                Cliente cliente = new Cliente
                {
                    IdCliente = clienteApi.IdCliente,
                    Nombre = clienteApi.NombreCliente,
                    Correo = clienteApi.CorreoElectronico,
                    Rfc = clienteApi.RfcCliente,
                    DomicilioFiscalReceptor = codigoPostalSat?.C_CodigoPostal,
                    RegimenFiscalReceptor = regimenFiscalSat?.C_RegimenFiscal,
                    EsActivo = clienteApi.IdStatusRegistro == 1,
                    FechaRegistro = clienteApi.FechaRegistro ?? DateTime.Now
                };

                if (!_facturacionServicio.ValidarDatosFiscalesCliente(cliente))
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "El cliente no tiene datos fiscales completos";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                FormaPagoApiDto? formaPagoApi = null;
                MetodoPagoApiDto? metodoPagoApi = null;
                UsoCFDIApiDto? usoCfdiApi = null;

                if (ventaApi.IdFormaPago.HasValue && ventaApi.IdFormaPago.Value > 0)
                    formaPagoApi = await _api.GetAsync<FormaPagoApiDto>($"api/formapagosat/{ventaApi.IdFormaPago.Value}");

                if (ventaApi.IdMetodoPago.HasValue && ventaApi.IdMetodoPago.Value > 0)
                    metodoPagoApi = await _api.GetAsync<MetodoPagoApiDto>($"api/metodopagosat/{ventaApi.IdMetodoPago.Value}");

                if (ventaApi.IdUsoCFDI.HasValue && ventaApi.IdUsoCFDI.Value > 0)
                    usoCfdiApi = await _api.GetAsync<UsoCFDIApiDto>($"api/usocfdisat/{ventaApi.IdUsoCFDI.Value}");

                string claveFormaPago = !string.IsNullOrWhiteSpace(formaPagoApi?.C_FormaPago)
                    ? formaPagoApi.C_FormaPago.Trim()
                    : "01";

                string claveMetodoPago = !string.IsNullOrWhiteSpace(metodoPagoApi?.C_MetodoPago)
                    ? metodoPagoApi.C_MetodoPago.Trim()
                    : "PUE";

                string claveUsoCFDI = !string.IsNullOrWhiteSpace(usoCfdiApi?.C_UsoCFDI)
                    ? usoCfdiApi.C_UsoCFDI.Trim()
                    : "S01";

                if (claveMetodoPago == "PPD" && claveFormaPago != "99")
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "Configuración inválida para facturar: si el método de pago es PPD, la forma de pago debe ser 99.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                if (claveUsoCFDI == "CP01")
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "Configuración inválida para facturar: el uso CFDI CP01 corresponde a complemento de pagos.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                var detallesParaTimbrar = new List<DetalleVenta>();

                foreach (var d in detalleVentaApi)
                {
                    var prodApi = productosApi.FirstOrDefault(p => p.IdProducto == d.IdProducto);

                    string? claveProdServ = null;
                    string? claveUnidad = null;

                    if (prodApi != null)
                    {
                        if (prodApi.IdClaveProdServSAT.HasValue)
                        {
                            var catProd = await _api.GetAsync<ClaveProdServSATDto>($"api/claveprodservsat/{prodApi.IdClaveProdServSAT.Value}");
                            claveProdServ = catProd?.C_ClaveProdServ;
                        }

                        if (prodApi.IdClaveUnidadSAT.HasValue)
                        {
                            var catUnidad = await _api.GetAsync<ClaveUnidadSATDto>($"api/claveunidadsat/{prodApi.IdClaveUnidadSAT.Value}");
                            claveUnidad = catUnidad?.C_ClaveUnidad;
                        }
                    }

                    decimal cantidad = d.Cantidad ?? 0m;
                    decimal precioUnitario = d.Precio ?? 0m;
                    decimal importeSinDescuento = Math.Round(cantidad * precioUnitario, 2, MidpointRounding.AwayFromZero);

                    decimal tasaDescuento = d.Descuento ?? 0m;
                    if (tasaDescuento > 1) tasaDescuento /= 100m;

                    decimal descuentoDinero = d.DescuentoDinero.HasValue && d.DescuentoDinero.Value > 0
                        ? Math.Round(d.DescuentoDinero.Value, 2, MidpointRounding.AwayFromZero)
                        : Math.Round(importeSinDescuento * tasaDescuento, 2, MidpointRounding.AwayFromZero);

                    decimal tasaImpuesto = d.Impuesto ?? 0m;
                    if (tasaImpuesto > 1) tasaImpuesto /= 100m;

                    decimal baseImpuesto = Math.Round(importeSinDescuento - descuentoDinero, 2, MidpointRounding.AwayFromZero);

                    decimal impuestoDinero = d.ImpuestoDinero.HasValue && d.ImpuestoDinero.Value > 0
                        ? Math.Round(d.ImpuestoDinero.Value, 2, MidpointRounding.AwayFromZero)
                        : Math.Round(baseImpuesto * tasaImpuesto, 2, MidpointRounding.AwayFromZero);

                    detallesParaTimbrar.Add(new DetalleVenta
                    {
                        IdProducto = d.IdProducto,
                        DescripcionProducto = prodApi?.Descripcion ?? $"Producto {d.IdProducto}",
                        Cantidad = cantidad,
                        Precio = precioUnitario,
                        SubTotal = importeSinDescuento,
                        Descuento = tasaDescuento,
                        DescuentoDinero = descuentoDinero,
                        Impuesto = tasaImpuesto,
                        ImpuestoDinero = impuestoDinero,
                        Total = Math.Round(baseImpuesto + impuestoDinero, 2, MidpointRounding.AwayFromZero),
                        ClaveProdServSat = claveProdServ,
                        ClaveUnidadSat = claveUnidad,
                        ObjetoImpuesto = "02",
                        TipoImpuesto = "002",
                        FactorImpuesto = "Tasa",
                        ValorImpuesto = tasaImpuesto > 0 ? tasaImpuesto : 0.160000m,
                        Unidad = "PZA"
                    });
                }

                Venta ventaParaTimbrar = new Venta
                {
                    IdVenta = ventaApi.IdVenta,
                    NumeroVenta = ventaApi.NumeroVenta,
                    IdTipoDocumentoVenta = 2,
                    IdCliente = ventaApi.IdCliente,
                    IdMetodoPago = ventaApi.IdMetodoPago,
                    IdUsoCFDI = ventaApi.IdUsoCFDI,
                    IdFormaPago = ventaApi.IdFormaPago,
                    ClaveFormaPago = claveFormaPago,
                    FormaPago = formaPagoApi != null ? $"{formaPagoApi.C_FormaPago} - {formaPagoApi.Descripcion}" : "01 - Efectivo",
                    ClaveMetodoPago = claveMetodoPago,
                    MetodoPago = metodoPagoApi != null ? $"{metodoPagoApi.C_MetodoPago} - {metodoPagoApi.Descripcion}" : "PUE - Pago en una sola exhibición",
                    ClaveUsoCFDI = claveUsoCFDI,
                    UsoCFDI = usoCfdiApi != null ? $"{usoCfdiApi.C_UsoCFDI} - {usoCfdiApi.Descripcion}" : "S01 - Sin efectos fiscales",
                    SubTotal = ventaApi.SubTotalVenta ?? 0m,
                    ImpuestoTotal = detalleVentaApi.Sum(x => x.ImpuestoDinero ?? 0m),
                    Total = ventaApi.TotalVenta ?? 0m,
                    FechaRegistro = ventaApi.FechaRegistroVenta ?? DateTime.Now,
                    NombreCliente = cliente.Nombre,
                    DocumentoCliente = cliente.Rfc,
                    DetalleVenta = detallesParaTimbrar
                };

                await CompletarCatalogosFiscales(detallesParaTimbrar, productosApi, ventaParaTimbrar);
                Negocio negocio = await _negocioService.Obtener();

                if (negocio == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se encontró la configuración del negocio";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                if (solicitud.SoloValidar)
                {
                    await _facturacionServicio.ValidarVenta(ventaParaTimbrar, cliente, negocio);
                    return Ok(new { estado = true, mensaje = "Validación completa. No se contactó al PAC ni se modificó la venta." });
                }
                string rutaFacturas = Path.Combine(_env.WebRootPath, "Facturas");
                var resultado = await _facturacionServicio.TimbrarVenta(ventaParaTimbrar, cliente, negocio, rutaFacturas);

                if (!resultado.exito)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "Error al timbrar: " + resultado.mensaje;
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                ventaApi.IdTipoDocumentoVenta = 2;
                ventaApi.Uuid = resultado.uuid;
                ventaApi.FechaTimbrado = DateTime.Now;
                ventaApi.IdFormaPago = ventaParaTimbrar.IdFormaPago;
                ventaApi.IdMetodoPago = ventaParaTimbrar.IdMetodoPago;
                ventaApi.IdUsoCFDI = ventaParaTimbrar.IdUsoCFDI;

                await ConfirmarFacturaApi(ventaApi);

                gResponse.Estado = true;
                gResponse.Mensaje = "Factura generada exitosamente";
                gResponse.Objeto = new
                {
                    numeroVenta = ventaApi.NumeroVenta,
                    uuid = resultado.uuid,
                    rutaPDF = resultado.rutaPDF,
                    rutaXML = resultado.rutaXML
                };

                return StatusCode(StatusCodes.Status200OK, gResponse);
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
                return StatusCode(StatusCodes.Status500InternalServerError, gResponse);
            }
        }

        private async Task<string> ClaveCatalogo(string catalogo, int? id, string campo)
        {
            if (id is null or <= 0) throw new InvalidOperationException($"Falta el catálogo {catalogo}.");
            var valores = await _api.GetAsync<Dictionary<string, System.Text.Json.JsonElement>>($"api/{catalogo}/{id}");
            string? clave = valores?.FirstOrDefault(x => x.Key.Equals(campo, StringComparison.OrdinalIgnoreCase)).Value.ToString();
            if (string.IsNullOrWhiteSpace(clave)) throw new InvalidOperationException($"El catálogo {catalogo}/{id} no tiene {campo}.");
            return clave.Trim();
        }

        private async Task CompletarCatalogosFiscales(IEnumerable<DetalleVenta> detalles, List<ProductoApiDto> productos, Venta venta)
        {
            venta.ClaveFormaPago = await ClaveCatalogo("formapagosat", venta.IdFormaPago, "c_FormaPago");
            venta.ClaveMetodoPago = await ClaveCatalogo("metodopagosat", venta.IdMetodoPago, "c_MetodoPago");
            venta.ClaveUsoCFDI = await ClaveCatalogo("usocfdisat", venta.IdUsoCFDI, "c_UsoCFDI");
            foreach (var detalle in detalles)
            {
                var producto = productos.FirstOrDefault(p => p.IdProducto == detalle.IdProducto)
                    ?? throw new InvalidOperationException($"Producto {detalle.IdProducto} no encontrado en la API.");
                detalle.ClaveProdServSat = await ClaveCatalogo("claveprodservsat", producto.IdClaveProdServSAT, "c_ClaveProdServ");
                detalle.ClaveUnidadSat = await ClaveCatalogo("claveunidadsat", producto.IdClaveUnidadSAT, "c_ClaveUnidad");
                detalle.ObjetoImpuesto = await ClaveCatalogo("objetoimpsat", producto.IdObjetoImpuestoSAT, "c_ObjetoImpuesto");
                detalle.ValorImpuesto = detalle.Impuesto ?? throw new InvalidOperationException("Falta la tasa de impuesto de la venta.");
                if (detalle.ObjetoImpuesto == "02")
                {
                    detalle.TipoImpuesto = await ClaveCatalogo("impuestosat", producto.IdImpuestoSAT, "c_Impuesto");
                    detalle.FactorImpuesto = await ClaveCatalogo("tipofactorsat", producto.IdTipoFactorSAT, "c_TipoFactor");
                    if (detalle.FactorImpuesto != "Tasa")
                        throw new InvalidOperationException("La implementación SOAP actual solo admite impuestos con factor Tasa.");
                }
                else if (detalle.ObjetoImpuesto != "01")
                    throw new InvalidOperationException("La implementación SOAP actual admite objetos de impuesto 01 y 02.");
                if (producto.IdMedidaLocal.HasValue)
                    detalle.Unidad = await ClaveCatalogo("medidalocal", producto.IdMedidaLocal, "c_MedidaLocal");
            }
        }

        private async Task ConfirmarFacturaApi(VentaApiDto venta)
        {
            if (string.IsNullOrWhiteSpace(venta.Uuid))
                throw new InvalidOperationException("No se puede confirmar una factura sin UUID.");
            await _api.PutAsync<VentaApiDto>("api/venta", venta);
            var confirmada = await _api.GetAsync<VentaApiDto>($"api/venta/{venta.IdVenta}");
            if (confirmada?.Uuid != venta.Uuid || confirmada.IdTipoDocumentoVenta != 2 || !confirmada.FechaTimbrado.HasValue)
                throw new InvalidOperationException("El PAC ya timbró y los archivos están respaldados, pero falta confirmar el estado por API. Reintente sincronizar la misma venta.");
        }

        public class SolicitudFactura
        {
            public bool SoloValidar { get; set; }
            public string NumeroVenta { get; set; }
            public int IdVenta { get; set; }
        }

        public class VentaApiDto
        {
            public int IdVenta { get; set; }
            public string? NumeroVenta { get; set; }
            public int? IdTipoDocumentoVenta { get; set; }
            public int? IdUsuario { get; set; }
            public int? IdCliente { get; set; }
            public decimal? SubTotalVenta { get; set; }
            public decimal? ImpuestoVenta { get; set; }
            public decimal? TotalVenta { get; set; }
            public DateTime? FechaRegistroVenta { get; set; }
            public int? IdTipoDeComprobante { get; set; }
            public int? IdMetodoPago { get; set; }
            public int? IdUsoCFDI { get; set; }
            public int? IdFormaPago { get; set; }
            public string? Uuid { get; set; }
            public DateTime? FechaTimbrado { get; set; }
        }

        public class DetalleVentaApiDto
        {
            public int IdDetalleVenta { get; set; }
            public int? IdVenta { get; set; }
            public int? IdProducto { get; set; }
            public decimal? Cantidad { get; set; }
            public decimal? Precio { get; set; }
            public decimal? SubTotal { get; set; }
            public decimal? Descuento { get; set; }
            public decimal? DescuentoDinero { get; set; }
            public decimal? Impuesto { get; set; }
            public decimal? ImpuestoDinero { get; set; }
            public decimal? Total { get; set; }
        }

        public class TipoDocumentoVentaApiDto
        {
            public int IdTipoDocumentoVenta { get; set; }
            public string? Descripcion { get; set; }
            public DateTime? FechaRegistro { get; set; }
            public int? IdStatusRegistro { get; set; }
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

            public string? Pais { get; set; }
            public string? Estado { get; set; }
            public string? Municipio { get; set; }

            public DateTime? FechaRegistro { get; set; }
            public int? IdStatusRegistro { get; set; }
        }

        public class ProductoApiDto
        {
            public int IdProducto { get; set; }
            public string? SkuCodigoDeBarras { get; set; }
            public int? IdMarca { get; set; }
            public int? IdModelo { get; set; }
            public string? Descripcion { get; set; }
            public int? IdCategoria { get; set; }
            public int? Stock { get; set; }
            public string? UrlImagen { get; set; }
            public string? NombreImagen { get; set; }
            public decimal? PrecioUnitarioCompra { get; set; }
            public decimal? PrecioUnitarioVenta { get; set; }
            public decimal? Descuento { get; set; }
            public int? IdMedidaLocal { get; set; }
            public int? IdClaveUnidadSAT { get; set; }
            public int? IdClaveProdServSAT { get; set; }
            public int? IdObjetoImpuestoSAT { get; set; }
            public int? IdImpuestoSAT { get; set; }
            public int? IdTipoFactorSAT { get; set; }
            public decimal? ImpuestoProducto { get; set; }
            public DateTime? FechaRegistro { get; set; }
            public int? IdStatusRegistro { get; set; }
        }

        public class UsuarioApiDto
        {
            public int IdUsuario { get; set; }
            public string? Nombre { get; set; }
            public string? Correo { get; set; }
            public int? IdRol { get; set; }
        }

        public class FormaPagoApiDto
        {
            public int IdFormaPago { get; set; }
            public string? C_FormaPago { get; set; }
            public string? Descripcion { get; set; }
            public int? IdStatusRegistro { get; set; }
        }

        public class MetodoPagoApiDto
        {
            public int IdMetodoPago { get; set; }
            public string? C_MetodoPago { get; set; }
            public string? Descripcion { get; set; }
            public int? IdStatusRegistro { get; set; }
        }

        public class UsoCFDIApiDto
        {
            public int IdUsoCFDI { get; set; }
            public string? C_UsoCFDI { get; set; }
            public string? Descripcion { get; set; }
            public int? IdStatusRegistro { get; set; }
        }

        public class CreaVentaApiDto
        {
            public int? IdUsuario { get; set; }
            public string? NumeroVenta { get; set; }
            public int? IdTipoDocumentoVenta { get; set; }
            public int? IdCliente { get; set; }
            public decimal? SubTotalVenta { get; set; }
            public decimal? ImpuestoVenta { get; set; }
            public decimal? TotalVenta { get; set; }
            public DateTime? FechaRegistroVenta { get; set; }

            public int? IdMetodoPago { get; set; }
            public int? IdUsoCFDI { get; set; }
            public int? IdFormaPago { get; set; }
        }

        public class CreaDetalleVentaApiDto
        {
            public int? IdVenta { get; set; }
            public int? IdProducto { get; set; }
            public decimal? Cantidad { get; set; }
            public decimal? Precio { get; set; }
            public decimal? SubTotal { get; set; }

            public decimal? Descuento { get; set; }
            public decimal? DescuentoDinero { get; set; }

            public decimal? Impuesto { get; set; }
            public decimal? ImpuestoDinero { get; set; }

            public decimal? Total { get; set; }
        }

        public class MarcaApiNuevaVentaDto
        {
            public int IdMarca { get; set; }
            public string? Descripcion { get; set; }
        }

        public class CategoriaApiNuevaVentaDto
        {
            public int IdCategoria { get; set; }
            public string? Descripcion { get; set; }
        }

        public class NumeroCorrelativoApiDto
        {
            public int IdNumeroCorrelativo { get; set; }
            public int? UltimoNumero { get; set; }
            public int? CantidadDigitos { get; set; }
            public string? Gestion { get; set; }
            public DateTime? FechaActualizacion { get; set; }
        }

        public class PaisApiDto
        {
            public int IdPais { get; set; }
            public string? Descripcion { get; set; }
        }

        public class EstadoApiDto
        {
            public int IdEstado { get; set; }
            public string? NombreDelEstado { get; set; }
        }

        public class MunicipioApiDto
        {
            public int IdMunicipio { get; set; }
            public string? Descripcion { get; set; }
        }

        public class CodigoPostalSATDto
        {
            public int IdCodigoPostal { get; set; }
            public string? C_CodigoPostal { get; set; }
            public int? IdEstado { get; set; }
            public int? IdMunicipio { get; set; }
            public int? IdLocalidad { get; set; }
            public string? estímuloFranjaFronteriza { get; set; }
            public DateTime? FechaInicioVigencia { get; set; }
            public DateTime? FechaFinVigencia { get; set; }
            public string? DescripcionDelHusoHorario { get; set; }
            public string? MesInicioHorarioVerano { get; set; }
            public string? DiaInicioHorarioVerano { get; set; }
            public TimeSpan? HoraInicioHorarioVerano { get; set; }
            public short? DiferenciaHorariaVerano { get; set; }
            public string? MesInicioHorarioInvierno { get; set; }
            public string? DiaInicioHorarioInvierno { get; set; }
            public TimeSpan? HoraInicioHorarioInvierno { get; set; }
            public short? DiferenciaHorariaInvierno { get; set; }
            public int? IdStatusRegistro { get; set; }
        }

        public class RegimenFiscalSATDto
        {
            public int IdRegimenFiscal { get; set; }
            public string? C_RegimenFiscal { get; set; }
            public string? Descripcion { get; set; }
            public string? Fisica { get; set; }
            public string? Moral { get; set; }
            public DateTime? FechaInicioVigencia { get; set; }
            public DateTime? FechaFinVigencia { get; set; }
            public int? IdStatusRegistro { get; set; }
        }

        public class ClaveProdServSATDto
        {
            public int IdClaveProdServ { get; set; }
            public string? C_ClaveProdServ { get; set; }
            public string? Descripcion { get; set; }
            public string? Incluir_IVA_trasladado { get; set; }
            public string? Incluir_IEPS_trasladado { get; set; }
            public string? Complemento_que_debe_incluir { get; set; }
            public DateTime? FechaInicioVigencia { get; set; }
            public DateTime? FechaFinVigencia { get; set; }
            public string? Estimulo_Franja_Fronteriza { get; set; }
            public string? Palabras_similares { get; set; }
            public int? IdStatusRegistro { get; set; }
        }

        public class ClaveUnidadSATDto
        {
            public int IdClaveUnidad { get; set; }
            public string? C_ClaveUnidad { get; set; }
            public string? Nombre { get; set; }
            public string? Descripcion { get; set; }
            public string? Nota { get; set; }
            public DateTime? FechaInicioVigencia { get; set; }
            public DateTime? FechaFinVigencia { get; set; }
            public string? Simbolo { get; set; }
            public int? IdStatusRegistro { get; set; }
        }
    }
}




