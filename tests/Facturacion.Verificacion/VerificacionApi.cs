using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using SistemaVenta.AplicacionWeb.Controllers;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.BLL.Implementacion;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.Entity.Models;

internal static class VerificacionApi
{
    public static async Task Ejecutar(string carpeta, Negocio negocio)
    {
        var api = new ApiSimulada();
        var pac = new PacSimulado();
        var servicio = new FacturacionService(null!, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Facturacion:TimbradoHabilitado"] = "true" }).Build(), pac);
        var controlador = new VentaController(new NegocioSimulado(negocio), servicio,
            null!, new EntornoSimulado(carpeta), api);
        var solicitud = new VentaController.SolicitudFactura { NumeroVenta = "200001", IdVenta = 200001, SoloValidar = true };
        bool Exito(IActionResult respuesta) => JsonSerializer.SerializeToElement(((ObjectResult)respuesta).Value)
            .EnumerateObject().First(x => x.Name.Equals("estado", StringComparison.OrdinalIgnoreCase)).Value.GetBoolean();
        void Comprobar(bool condicion, string caso)
        {
            if (!condicion) throw new Exception("FALLO: " + caso);
            Console.WriteLine("PASS: " + caso);
        }
        solicitud.IdVenta = 999;
        Comprobar(!Exito(await controlador.SolicitarFactura(solicitud)) && pac.Llamadas == 0 && api.Escrituras == 0,
            "ID y folio distintos rechazados antes del PAC");
        solicitud.IdVenta = 200001;
        Comprobar(Exito(await controlador.SolicitarFactura(solicitud)) && pac.Llamadas == 0 && api.Escrituras == 0,
            "flujo API de prevalidación lee venta, cliente, productos y catálogos sin escrituras");
        solicitud.SoloValidar = false;
        api.FallarActualizacion = true;
        Comprobar(!Exito(await controlador.SolicitarFactura(solicitud)) && pac.Llamadas == 1 && api.Venta.Uuid == null,
            "fallo de actualización API conserva factura y no confirma la venta");
        api.FallarActualizacion = false;
        Comprobar(Exito(await controlador.SolicitarFactura(solicitud)) && pac.Llamadas == 1 && api.Venta.IdTipoDocumentoVenta == 2 && api.Venta.FechaTimbrado.HasValue,
            "recuperación sincroniza UUID, fecha y tipo por API sin volver a timbrar");
        Comprobar(!Exito(await controlador.SolicitarFactura(solicitud)) && pac.Llamadas == 1,
            "venta con UUID rechaza solicitud duplicada");

        var apiNueva = new ApiSimulada { PermitirAlta = true };
        var pacNuevo = new PacSimulado();
        var servicioNuevo = new FacturacionService(null!, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Facturacion:TimbradoHabilitado"] = "true" }).Build(), pacNuevo);
        var nuevo = new VentaController(new NegocioSimulado(negocio), servicioNuevo, null!, new EntornoSimulado(carpeta), apiNueva);
        nuevo.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1") }, "prueba"))
        } };
        var modelo = new SistemaVenta.AplicacionWeb.Models.ViewModels.VMVenta
        {
            IdTipoDocumentoVenta = 2, IdCliente = 1, IdFormaPago = 1, IdMetodoPago = 1, IdUsoCFDI = 1,
            DetalleVenta = new() { new() { IdProducto = 1, DescripcionProducto = "PRUEBA API", Cantidad = 1, Precio = 100, Impuesto = .16m } }
        };
        var alta = await nuevo.RegistrarVenta(modelo);
        Comprobar(Exito(alta) && pacNuevo.Llamadas == 1 && apiNueva.TipoAlCrear == 1 && apiNueva.Venta.IdTipoDocumentoVenta == 2,
            "factura nueva se registra pendiente y confirma únicamente mediante API después del PAC");
    }

    private sealed class NegocioSimulado(Negocio negocio) : INegocioService
    {
        public Task<Negocio> Obtener() => Task.FromResult(negocio);
        public Task<Negocio> GuardarCambios(Negocio entidad, Stream Logo = null!, string NombreLogo = "") => throw new NotSupportedException();
    }
    private sealed class EntornoSimulado(string carpeta) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = carpeta;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Verificacion";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = carpeta;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private sealed class ApiSimulada : IApiClient
    {
        public bool FallarActualizacion;
        public bool PermitirAlta;
        public int? TipoAlCrear;
        public int Escrituras;
        public VentaController.VentaApiDto Venta = new()
        {
            IdVenta = 200001, NumeroVenta = "200001", IdCliente = 1, IdTipoDocumentoVenta = 1,
            SubTotalVenta = 100, ImpuestoVenta = 16, TotalVenta = 116, IdFormaPago = 1, IdMetodoPago = 1, IdUsoCFDI = 1
        };
        public Task<T?> GetAsync<T>(string endpoint)
        {
            object valor = endpoint switch
            {
                "api/venta" => new[] { Venta },
                "api/venta/200001" => Venta,
                "api/venta/300001" => Venta,
                "api/usuario" => new[] { new { IdUsuario = 1, Nombre = "PRUEBA" } },
                "api/numerocorrelativo" => new[] { new { IdNumeroCorrelativo = 1, UltimoNumero = 300000, CantidadDigitos = 6, Gestion = "venta" } },
                "api/detalleventa" => new[] { new { IdVenta = 200001, IdProducto = 1, Cantidad = 1m, Precio = 100m, SubTotal = 100m, Impuesto = .16m, ImpuestoDinero = 16m, Total = 116m } },
                "api/producto" => new[] { new { IdProducto = 1, Descripcion = "PRUEBA API", IdClaveProdServSAT = 1, IdClaveUnidadSAT = 1, IdObjetoImpuestoSAT = 1, IdImpuestoSAT = 1, IdTipoFactorSAT = 1 } },
                "api/cliente/1" => new { IdCliente = 1, NombreCliente = "PUBLICO", RfcCliente = "XAXX010101000", IdCodigoPostal = 1, IdRegimenFiscal = 1 },
                "api/codigopostalsat/1" => new { C_CodigoPostal = "01000" },
                "api/regimenfiscalsat/1" => new { C_RegimenFiscal = "616" },
                "api/formapagosat/1" => new { C_FormaPago = "01" },
                "api/metodopagosat/1" => new { C_MetodoPago = "PUE" },
                "api/usocfdisat/1" => new { C_UsoCFDI = "S01" },
                "api/claveprodservsat/1" => new { C_ClaveProdServ = "01010101" },
                "api/claveunidadsat/1" => new { C_ClaveUnidad = "H87" },
                "api/objetoimpsat/1" => new { C_ObjetoImpuesto = "02" },
                "api/impuestosat/1" => new { C_Impuesto = "002" },
                "api/tipofactorsat/1" => new { C_TipoFactor = "Tasa" },
                _ => throw new InvalidOperationException("Endpoint inesperado: " + endpoint)
            };
            return Task.FromResult(JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(valor), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
        }
        public Task<T?> PutAsync<T>(string endpoint, object body)
        {
            Escrituras++;
            if (PermitirAlta && endpoint == "api/numerocorrelativo")
                return Task.FromResult(JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(body)));
            if (FallarActualizacion) throw new IOException("Fallo API simulado");
            if (endpoint != "api/venta") throw new Exception("Escritura inesperada");
            Venta = JsonSerializer.Deserialize<VentaController.VentaApiDto>(JsonSerializer.Serialize(body))!;
            return Task.FromResult(JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(Venta)));
        }
        public Task<T?> PostAsync<T>(string endpoint, object body)
        {
            if (!PermitirAlta) throw new Exception("POST no permitido en esta prueba");
            Escrituras++;
            if (endpoint == "api/venta")
            {
                Venta = JsonSerializer.Deserialize<VentaController.VentaApiDto>(JsonSerializer.Serialize(body))!;
                Venta.IdVenta = 300001;
                TipoAlCrear = Venta.IdTipoDocumentoVenta;
                return Task.FromResult(JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(Venta)));
            }
            if (endpoint == "api/detalleventa")
                return Task.FromResult(JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(body)));
            throw new Exception("POST inesperado: " + endpoint);
        }
        public Task<bool> DeleteAsync(string endpoint) => throw new Exception("DELETE no permitido");
    }
}
