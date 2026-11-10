using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.AplicacionWeb.Utilidades.Response;

namespace SistemaVenta.AplicacionWeb.Controllers
{
    [Authorize]
    public class ProductoController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IApiClient _api;
        private readonly IWebHostEnvironment _environment;

        public ProductoController(IMapper mapper, IApiClient api, IWebHostEnvironment environment)
        {
            _mapper = mapper;
            _api = api;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Lista()
        {
            GenericResponse<List<VMProducto>> gResponse = new();

            try
            {
                var productos = await _api.GetAsync<List<ProductoApiDto>>("api/producto?skip=0&take=99999");
                var catalogos = await ObtenerCatalogosAsync();

                List<VMProducto> vmProductoLista = new();

                foreach (var producto in productos ?? new List<ProductoApiDto>())
                {
                    vmProductoLista.Add(MapearProductoApiAVm(producto, catalogos));
                }

                gResponse.Estado = true;
                gResponse.Objeto = vmProductoLista;
            }
            catch (Exception)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.";
                gResponse.Objeto = new List<VMProducto>();
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromForm] IFormFile? imagen, [FromForm] string modelo)
        {
            GenericResponse<VMProducto> gResponse = new();

            try
            {
                VMProducto vmProducto = JsonConvert.DeserializeObject<VMProducto>(modelo)!;

                var (urlImagen, nombreImagen) = await GuardarImagenLocal(imagen);

                ProductoApiDto dto = new()
                {
                    SkuCodigoDeBarras = vmProducto.CodigoBarra,
                    Descripcion = vmProducto.Descripcion,
                    IdCategoria = vmProducto.IdCategoria,
                    Stock = vmProducto.Stock,
                    UrlImagen = urlImagen,
                    NombreImagen = nombreImagen,
                    PrecioUnitarioVenta = decimal.TryParse(vmProducto.Precio, out var precioVenta) ? precioVenta : 0,
                    IdStatusRegistro = vmProducto.EsActivo,
                    ImpuestoProducto = vmProducto.ValorImpuesto,
                    Descuento = vmProducto.Descuento,
                    IdMarca = vmProducto.IdMarca,
                    Idmodelo = vmProducto.IdModelo,
                    IdMedidaLocal = vmProducto.IdMedidaLocal,
                    IdClaveUnidadSAT = vmProducto.IdClaveUnidadSAT,
                    IdClaveProdServSAT = vmProducto.IdClaveProdServSAT,
                    IdObjetoImpuestoSAT = vmProducto.IdObjetoImpuestoSAT,
                    IdImpuestoSAT = vmProducto.IdImpuestoSAT,
                    IdTipoFactorSAT = vmProducto.IdTipoFactorSAT,
                    IdMoneda = vmProducto.IdMoneda,
                    FechaRegistro = DateTime.Now
                };

                ProductoApiDto? creado = await _api.PostAsync<ProductoApiDto>("api/producto", dto);

                if (creado == null)
                    throw new Exception("No se pudo crear el producto en la API.");

                var catalogos = await ObtenerCatalogosAsync();

                gResponse.Estado = true;
                gResponse.Objeto = MapearProductoApiAVm(creado, catalogos);
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpPut]
        public async Task<IActionResult> Editar([FromForm] IFormFile? imagen, [FromForm] string modelo)
        {
            GenericResponse<VMProducto> gResponse = new();

            try
            {
                VMProducto vmProducto = JsonConvert.DeserializeObject<VMProducto>(modelo)!;

                int? idMarca = vmProducto.IdMarca;

                if (idMarca == null || idMarca == 0)
                {
                    var marcasBuscar = await _api.GetAsync<List<MarcaApiDto>>("api/marca?skip=0&take=99999");

                    idMarca = marcasBuscar?
                        .FirstOrDefault(m =>
                            !string.IsNullOrWhiteSpace(m.C_Marca) &&
                            !string.IsNullOrWhiteSpace(vmProducto.Marca) &&
                            m.C_Marca.Trim().ToUpper() == vmProducto.Marca.Trim().ToUpper()
                        )?.IdMarca;
                }

                string? urlImagen = vmProducto.UrlImagen;
                string? nombreImagen = vmProducto.NombreImagen;

                if (imagen != null && imagen.Length > 0)
                {
                    var nuevaImagen = await GuardarImagenLocal(imagen);
                    urlImagen = nuevaImagen.urlImagen;
                    nombreImagen = nuevaImagen.nombreImagen;
                }

                ProductoApiDto dto = new()
                {
                    IdProducto = vmProducto.IdProducto,
                    SkuCodigoDeBarras = vmProducto.CodigoBarra,
                    Descripcion = vmProducto.Descripcion,
                    IdCategoria = vmProducto.IdCategoria,
                    Stock = vmProducto.Stock,
                    UrlImagen = urlImagen,
                    NombreImagen = nombreImagen,
                    PrecioUnitarioVenta = decimal.TryParse(vmProducto.Precio, out var precioVenta) ? precioVenta : 0,
                    IdStatusRegistro = vmProducto.EsActivo,
                    ImpuestoProducto = vmProducto.ValorImpuesto,
                    Descuento = vmProducto.Descuento,
                    IdMarca = idMarca,
                    Idmodelo = vmProducto.IdModelo,
                    IdMedidaLocal = vmProducto.IdMedidaLocal,
                    IdClaveUnidadSAT = vmProducto.IdClaveUnidadSAT,
                    IdClaveProdServSAT = vmProducto.IdClaveProdServSAT,
                    IdObjetoImpuestoSAT = vmProducto.IdObjetoImpuestoSAT,
                    IdImpuestoSAT = vmProducto.IdImpuestoSAT,
                    IdTipoFactorSAT = vmProducto.IdTipoFactorSAT,
                    IdMoneda = vmProducto.IdMoneda
                };

                ProductoApiDto? editado = await _api.PutAsync<ProductoApiDto>("api/producto", dto);

                if (editado == null)
                    throw new Exception("No se pudo editar el producto en la API.");

                var catalogos = await ObtenerCatalogosAsync();

                gResponse.Estado = true;
                gResponse.Objeto = MapearProductoApiAVm(editado, catalogos);
            }
            catch (Exception ex)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = ex.Message;
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        [HttpDelete]
        public async Task<IActionResult> Eliminar(int IdProducto)
        {
            GenericResponse<string> gResponse = new();

            try
            {
                gResponse.Estado = await _api.DeleteAsync($"api/producto/{IdProducto}");

                if (!gResponse.Estado)
                {
                    gResponse.Mensaje = "No se pudo eliminar el producto.";
                }
            }
            catch (Exception)
            {
                gResponse.Estado = false;
                gResponse.Mensaje = "No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.";
            }

            return StatusCode(StatusCodes.Status200OK, gResponse);
        }

        private async Task<CatalogosProductoVm> ObtenerCatalogosAsync()
        {
            return new CatalogosProductoVm
            {
                Marcas = await _api.GetAsync<List<MarcaApiDto>>("api/marca?skip=0&take=99999"),
                Modelos = await _api.GetAsync<List<ModeloApiDto>>("api/modelo?skip=0&take=99999"),
                Categorias = await _api.GetAsync<List<CategoriaApiDtoo>>("api/categoria?skip=0&take=99999"),
                Unidades = await _api.GetAsync<List<ClaveUnidadSATApiDtoo>>("api/claveunidadsat?skip=0&take=99999"),
                ProductosSat = await _api.GetAsync<List<ClaveProdServSATApiDtoo>>("api/claveprodservsat?skip=0&take=99999"),
                Objetos = await _api.GetAsync<List<ObjetoImpuestoSATApiDtoo>>("api/objetoimpsat?skip=0&take=99999"),
                Impuestos = await _api.GetAsync<List<ImpuestoSATApiDtoo>>("api/impuestosat?skip=0&take=99999"),
                Factores = await _api.GetAsync<List<TipoFactorSATApiDtoo>>("api/tipofactorsat?skip=0&take=99999"),
                Monedas = await _api.GetAsync<List<MonedaSATApiDto>>("api/monedasat?skip=0&take=99999")
            };
        }

        private async Task<(string? urlImagen, string? nombreImagen)> GuardarImagenLocal(IFormFile? imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return (null, null);

            string carpeta = Path.Combine(_environment.WebRootPath, "imagenes", "productos");

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            string extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();

            string[] extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
            if (!extensionesPermitidas.Contains(extension))
                throw new Exception("Formato de imagen no válido. Solo se permiten .jpg, .jpeg, .png y .webp.");

            string nombreArchivo = $"{Guid.NewGuid():N}{extension}";
            string rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                await imagen.CopyToAsync(stream);
            }

            string urlPublica = $"/imagenes/productos/{nombreArchivo}";
            return (urlPublica, nombreArchivo);
        }

        private static VMProducto MapearProductoApiAVm(
            ProductoApiDto x,
            CatalogosProductoVm catalogos
        )
        {
            return new VMProducto
            {
                IdProducto = x.IdProducto,
                CodigoBarra = x.SkuCodigoDeBarras,
                Descripcion = x.Descripcion,
                IdCategoria = x.IdCategoria,
                Stock = x.Stock,
                UrlImagen = x.UrlImagen,
                NombreImagen = x.NombreImagen,
                Precio = x.PrecioUnitarioVenta?.ToString("0.00"),
                EsActivo = x.IdStatusRegistro,
                ValorImpuesto = x.ImpuestoProducto,
                Descuento = x.Descuento,
                IdMarca = x.IdMarca,
                IdModelo = x.Idmodelo,
                IdMedidaLocal = x.IdMedidaLocal,
                IdClaveUnidadSAT = x.IdClaveUnidadSAT,
                IdClaveProdServSAT = x.IdClaveProdServSAT,
                IdObjetoImpuestoSAT = x.IdObjetoImpuestoSAT,
                IdImpuestoSAT = x.IdImpuestoSAT,
                IdTipoFactorSAT = x.IdTipoFactorSAT,
                IdMoneda = x.IdMoneda,
                Moneda = catalogos.Monedas?
                    .FirstOrDefault(m => m.IdMoneda == x.IdMoneda)?
                    .CMoneda ?? "",
                FechaRegistro = x.FechaRegistro,
                FechaRegistroTexto = x.FechaRegistro?.ToString("dd/MM/yyyy HH:mm"),

                NombreCategoria = catalogos.Categorias?
                    .FirstOrDefault(c => c.IdCategoria == x.IdCategoria)?
                    .Descripcion ?? "",

                Marca = catalogos.Marcas?
                    .FirstOrDefault(m => m.IdMarca == x.IdMarca)?
                    .C_Marca ?? "",

                Modelo = catalogos.Modelos?
                    .FirstOrDefault(m => m.IdModelo == x.Idmodelo)?
                    .C_Modelo ?? "",

                MedidaSat = catalogos.Unidades?
                    .FirstOrDefault(u => u.IdClaveUnidad == x.IdClaveUnidadSAT)?
                    .C_ClaveUnidad ?? "0",

                ClaveProductoSat = catalogos.ProductosSat?
                    .FirstOrDefault(p => p.IdClaveProdServ == x.IdClaveProdServSAT)?
                    .C_ClaveProdServ ?? "0",

                ObjetoImpuesto = catalogos.Objetos?
                    .FirstOrDefault(o => o.IdObjetoImpSAT == x.IdObjetoImpuestoSAT)?
                    .C_ObjetoImp ?? "0",

                Impuesto = catalogos.Impuestos?
                    .FirstOrDefault(i => i.IdImpuesto == x.IdImpuestoSAT)?
                    .C_Impuesto ?? "0",

                FactorImpuesto = catalogos.Factores?
                    .FirstOrDefault(f => f.IdTipoFactor == x.IdTipoFactorSAT)?
                    .C_TipoFactor ?? "0",

                MedidaEmpresa = x.IdMedidaLocal switch
                {
                    1 => "PZA",
                    2 => "XBX",
                    3 => "BG",
                    4 => "RO",
                    5 => "LTR",
                    6 => "MTR",
                    7 => "KGM",
                    8 => "GRM",
                    _ => "0"
                }
            };
        }
    }

    public class CatalogosProductoVm
    {
        public List<MarcaApiDto>? Marcas { get; set; }
        public List<ModeloApiDto>? Modelos { get; set; }
        public List<CategoriaApiDtoo>? Categorias { get; set; }
        public List<ClaveUnidadSATApiDtoo>? Unidades { get; set; }
        public List<ClaveProdServSATApiDtoo>? ProductosSat { get; set; }
        public List<ObjetoImpuestoSATApiDtoo>? Objetos { get; set; }
        public List<ImpuestoSATApiDtoo>? Impuestos { get; set; }
        public List<TipoFactorSATApiDtoo>? Factores { get; set; }
        public List<MonedaSATApiDto>? Monedas { get; set; }
    }

    public class ProductoApiDto
    {
        public int IdProducto { get; set; }
        public string? SkuCodigoDeBarras { get; set; }
        public int? IdMarca { get; set; }
        public int? Idmodelo { get; set; }
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
        public int? IdMoneda { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    public class ModeloApiDto
    {
        public int IdModelo { get; set; }
        public string? C_Modelo { get; set; }
        public int? IdMarca { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    public class MarcaApiDto
    {
        public int IdMarca { get; set; }
        public string? C_Marca { get; set; }
    }

    public class CategoriaApiDtoo
    {
        public int IdCategoria { get; set; }
        public string? Descripcion { get; set; }
    }

    public class ClaveUnidadSATApiDtoo
    {
        public int IdClaveUnidad { get; set; }
        public string? C_ClaveUnidad { get; set; }
    }

    public class ClaveProdServSATApiDtoo
    {
        public int IdClaveProdServ { get; set; }
        public string? C_ClaveProdServ { get; set; }
    }

    public class ObjetoImpuestoSATApiDtoo
    {
        public int IdObjetoImpSAT { get; set; }
        public string? C_ObjetoImp { get; set; }
    }

    public class ImpuestoSATApiDtoo
    {
        public int IdImpuesto { get; set; }
        public string? C_Impuesto { get; set; }
    }

    public class TipoFactorSATApiDtoo
    {
        public int IdTipoFactor { get; set; }
        public string? C_TipoFactor { get; set; }
    }

    public class MonedaSATApiDto
    {
        public int IdMoneda { get; set; }
        public string? CMoneda { get; set; }
        public string? Descripcion { get; set; }
    }
}