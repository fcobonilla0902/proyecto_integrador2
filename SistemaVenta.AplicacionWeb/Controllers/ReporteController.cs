using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.AplicacionWeb.Utilidades.Response;
using System.Globalization;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class ReporteController : Controller
    {
        private readonly IApiClient _apiClient;

        public ReporteController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ReporteVenta(string fechaInicio, string fechaFin)
        {
            var gResponse = new GenericResponse<List<VMReporteVenta>>
            {
                Estado = false,
                Objeto = new List<VMReporteVenta>()
            };

            try
            {
                if (string.IsNullOrWhiteSpace(fechaInicio) || string.IsNullOrWhiteSpace(fechaFin))
                {
                    gResponse.Mensaje = "Debe proporcionar fechaInicio y fechaFin";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                DateTime inicio = DateTime.ParseExact(fechaInicio, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime fin = DateTime.ParseExact(fechaFin, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var ventas = await _apiClient.GetAsync<List<VentaDto>>("api/venta");
                var detalles = await _apiClient.GetAsync<List<DetalleVentaDto>>("api/detalleventa");
                var clientes = await _apiClient.GetAsync<List<ClienteDto>>("api/cliente");
                var productos = await _apiClient.GetAsync<List<ProductoDto>>("api/producto");
                var tiposDocumento = await _apiClient.GetAsync<List<TipoDocumentoVentaDto>>("api/tipodocumentoventa");

                if (ventas == null || detalles == null || clientes == null || productos == null || tiposDocumento == null)
                {
                    gResponse.Mensaje = "No se pudo conectar con la API o alguna colección regresó null";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                var ventasFiltradas = ventas
                    .Where(v => v.FechaRegistroVenta.HasValue &&
                                v.FechaRegistroVenta.Value.Date >= inicio.Date &&
                                v.FechaRegistroVenta.Value.Date <= fin.Date)
                    .ToList();

                var reporte = (
                    from d in detalles
                    join v in ventasFiltradas on d.IdVenta equals v.IdVenta
                    join c in clientes on v.IdCliente equals c.IdCliente into clienteJoin
                    from c in clienteJoin.DefaultIfEmpty()
                    join p in productos on d.IdProducto equals p.IdProducto into productoJoin
                    from p in productoJoin.DefaultIfEmpty()
                    join t in tiposDocumento on v.IdTipoDocumentoVenta equals t.IdTipoDocumentoVenta into tipoJoin
                    from t in tipoJoin.DefaultIfEmpty()
                    select new VMReporteVenta
                    {
                        FechaRegistro = v.FechaRegistroVenta?.ToString("dd/MM/yyyy") ?? "",
                        NumeroVenta = v.NumeroVenta ?? "",
                        TipoDocumento = t?.Descripcion ?? "",
                        DocumentoCliente = c?.RfcCliente ?? "",
                        NombreCliente = c?.NombreCliente ?? "",
                        SubTotalVenta = (v.SubTotalVenta ?? 0).ToString("0.00"),
                        ImpuestoTotalVenta = (v.ImpuestoVenta ?? 0).ToString("0.00"),
                        TotalVenta = (v.TotalVenta ?? 0).ToString("0.00"),
                        Producto = p?.Descripcion ?? "",
                        Cantidad = Convert.ToInt32(d.Cantidad ?? 0),
                        Precio = (d.Precio ?? 0).ToString("0.00"),
                        Total = (d.Total ?? 0).ToString("0.00")
                    }
                ).ToList();

                gResponse.Estado = true;
                gResponse.Objeto = reporte;
                gResponse.Mensaje = $"Se encontraron {reporte.Count} registros";
            }
            catch (FormatException)
            {
                gResponse.Mensaje = "Formato de fecha inválido. Debe ser dd/MM/yyyy";
            }
            catch (Exception ex)
            {
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }
    }

    public class VentaDto
    {
        public int IdVenta { get; set; }
        public string? NumeroVenta { get; set; }
        public int? IdTipoDocumentoVenta { get; set; }
        public int? IdCliente { get; set; }
        public decimal? SubTotalVenta { get; set; }
        public decimal? ImpuestoVenta { get; set; }
        public decimal? TotalVenta { get; set; }
        public DateTime? FechaRegistroVenta { get; set; }
    }

    public class DetalleVentaDto
    {
        public int IdDetalleVenta { get; set; }
        public int? IdVenta { get; set; }
        public int? IdProducto { get; set; }
        public decimal? Cantidad { get; set; }
        public decimal? Precio { get; set; }
        public decimal? Total { get; set; }
        public decimal? DescuentoDinero { get; internal set; }
    }

    public class ClienteDto
    {
        public int IdCliente { get; set; }
        public string? NombreCliente { get; set; }
        public string? RfcCliente { get; set; }
    }

    public class ProductoDto
    {
        public int IdProducto { get; set; }
        public string? Descripcion { get; set; }
    }

    public class TipoDocumentoVentaDto
    {
        public int IdTipoDocumentoVenta { get; set; }
        public string? Descripcion { get; set; }
    }
}