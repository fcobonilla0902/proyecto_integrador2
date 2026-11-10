using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.AplicacionWeb.Utilidades.Response;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class DashBoardController : Controller
    {
        private readonly IApiClient _api;

        public DashBoardController(IApiClient api)
        {
            _api = api;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerResumen()
        {
            GenericResponse<VMDashBoard> gResponse = new GenericResponse<VMDashBoard>();

            try
            {
                DashboardApiDto? dashboardApi = await _api.GetAsync<DashboardApiDto>("api/dashboard/resumen");

                if (dashboardApi == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se pudo obtener el resumen del dashboard.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                VMDashBoard vmDashBoard = new VMDashBoard
                {
                    TotalVentas = dashboardApi.TotalVentas,
                    TotalIngresos = dashboardApi.TotalIngresos,
                    TotalProductos = dashboardApi.TotalProductos,
                    TotalCategorias = dashboardApi.TotalCategorias,

                    VentasUltimaSemana = dashboardApi.VentasUltimaSemana?
                        .Select(x => new VMVentasSemana
                        {
                            Fecha = x.Fecha,
                            Total = x.Total
                        })
                        .ToList() ?? new List<VMVentasSemana>(),

                    ProductosTopUltimaSemana = dashboardApi.ProductosTopUltimaSemana?
                        .Select(x => new VMProductosSemana
                        {
                            Producto = x.Producto,
                            Cantidad = x.Cantidad
                        })
                        .ToList() ?? new List<VMProductosSemana>()
                };

                gResponse.Estado = true;
                gResponse.Objeto = vmDashBoard;
            }
            catch (Exception)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.";
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }
    }

    public class DashboardApiDto
    {
        public int TotalVentas { get; set; }
        public string? TotalIngresos { get; set; }
        public int TotalProductos { get; set; }
        public int TotalCategorias { get; set; }

        public List<VentasSemanaApiDto> VentasUltimaSemana { get; set; } = new();
        public List<ProductosTopSemanaApiDto> ProductosTopUltimaSemana { get; set; } = new();
    }

    public class VentasSemanaApiDto
    {
        public string? Fecha { get; set; }
        public int Total { get; set; }
    }

    public class ProductosTopSemanaApiDto
    {
        public string? Producto { get; set; }
        public int Cantidad { get; set; }
    }
}