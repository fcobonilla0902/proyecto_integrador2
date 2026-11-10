using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.AplicacionWeb.Utilidades.Response;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class CategoriaController : Controller
    {
        private readonly IApiClient _api;

        public CategoriaController(IApiClient api)
        {
            _api = api;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Lista()
        {
            GenericResponse<List<VMCategoria>> gResponse = new();

            try
            {
                List<CategoriaApiDto>? categoriasApi = await _api.GetAsync<List<CategoriaApiDto>>("api/categoria");

                gResponse.Estado = true;
                gResponse.Objeto = categoriasApi?
                    .Select(x => new VMCategoria
                    {
                        IdCategoria = x.IdCategoria,
                        Descripcion = x.Descripcion,
                        esActivo = x.IdStatusRegistro
                    })
                    .ToList() ?? new List<VMCategoria>();
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.";
                gResponse.Objeto = new List<VMCategoria>();
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] VMCategoria modelo)
        {
            GenericResponse<VMCategoria> gResponse = new GenericResponse<VMCategoria>();

            try
            {
                CreaCategoriaApiDto dto = new CreaCategoriaApiDto
                {
                    Descripcion = modelo.Descripcion,
                    FechaRegistro = DateTime.Now,
                    IdStatusRegistro = modelo.esActivo ?? 1
                };

                CategoriaApiDto? categoriaCreada = await _api.PostAsync<CategoriaApiDto>("api/categoria", dto);

                if (categoriaCreada == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se pudo crear la categoría.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                gResponse.Estado = true;
                gResponse.Objeto = new VMCategoria
                {
                    IdCategoria = categoriaCreada.IdCategoria,
                    Descripcion = categoriaCreada.Descripcion,
                    esActivo = categoriaCreada.IdStatusRegistro
                };
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpPut]
        public async Task<IActionResult> Editar([FromBody] VMCategoria modelo)
        {
            GenericResponse<VMCategoria> gResponse = new GenericResponse<VMCategoria>();

            try
            {
                CategoriaApiDto dto = new CategoriaApiDto
                {
                    IdCategoria = modelo.IdCategoria,
                    Descripcion = modelo.Descripcion,
                    FechaRegistro = DateTime.Now,
                    IdStatusRegistro = modelo.esActivo ?? 1
                };

                CategoriaApiDto? categoriaEditada = await _api.PutAsync<CategoriaApiDto>("api/categoria", dto);

                if (categoriaEditada == null)
                {
                    gResponse.Estado = false;
                    gResponse.Mensaje = "No se pudo editar la categoría.";
                    return StatusCode(StatusCodes.Status200OK, gResponse);
                }

                gResponse.Estado = true;
                gResponse.Objeto = new VMCategoria
                {
                    IdCategoria = categoriaEditada.IdCategoria,
                    Descripcion = categoriaEditada.Descripcion,
                    esActivo = categoriaEditada.IdStatusRegistro
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
        public async Task<IActionResult> Eliminar(int idCategoria)
        {
            GenericResponse<string> gResponse = new GenericResponse<string>();

            try
            {
                gResponse.Estado = await _api.DeleteAsync($"api/categoria/{idCategoria}");

                if (!gResponse.Estado)
                {
                    gResponse.Mensaje = "No se pudo eliminar la categoría.";
                }
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }
    }

    public class CategoriaApiDto
    {
        public int IdCategoria { get; set; }
        public string? Descripcion { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    public class CreaCategoriaApiDto
    {
        public string? Descripcion { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public int? IdStatusRegistro { get; set; }
    }
}