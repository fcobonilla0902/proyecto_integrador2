using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaVenta.AplicacionWeb.Controllers;
using SistemaVenta.AplicacionWeb.Services.Api;
using SistemaVenta.BLL.Implementacion;

internal static class VerificacionApiReal
{
    public static async Task Ejecutar()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiSettings:BaseUrl"] = "http://localhost:5096/",
            ["Facturacion:TimbradoHabilitado"] = "false"
        }).Build();
        using var proveedor = new ServiceCollection().AddHttpClient().BuildServiceProvider();
        var factory = proveedor.GetRequiredService<IHttpClientFactory>();
        var negocio = new NegocioService(null!, configuracion, factory);
        using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5096/"), Timeout = TimeSpan.FromSeconds(20) };
        IApiClient api = new SoloLecturaApi(new ApiClient(http));
        var pac = new PacSimulado { Fallar = true };
        var facturacion = new FacturacionService(negocio, configuracion, pac);
        // SoloValidar retorna antes de acceder al entorno o escribir archivos.
        var controller = new VentaController(negocio, facturacion, null!, null!, api);
        var ventas = await api.GetAsync<List<VentaController.VentaApiDto>>("api/venta") ?? new();
        var candidatas = ventas.Where(v => v.IdTipoDocumentoVenta == 1 && string.IsNullOrWhiteSpace(v.Uuid) && v.IdCliente > 0)
            .OrderByDescending(v => v.IdVenta).Take(5).ToList();
        if (candidatas.Count == 0) throw new Exception("No hay tickets sin UUID con cliente para validar.");
        foreach (var venta in candidatas)
        {
            var resultado = (ObjectResult)await controller.SolicitarFactura(new()
            {
                IdVenta = venta.IdVenta, NumeroVenta = venta.NumeroVenta!, SoloValidar = true
            });
            var campos = JsonSerializer.SerializeToElement(resultado.Value).EnumerateObject().ToDictionary(p => p.Name.ToLowerInvariant(), p => p.Value);
            bool exito = campos["estado"].GetBoolean();
            Console.WriteLine($"API real: venta {venta.IdVenta}, validación={exito}, {campos["mensaje"].GetString()}");
            if (pac.Llamadas != 0) throw new Exception("Se intentó contactar al PAC durante la prevalidación.");
            if (exito)
            {
                Console.WriteLine("PASS: flujo con API real validado sin escrituras y sin PAC.");
                return;
            }
        }
        throw new Exception("Los tickets consultados no superaron la validación; revise los mensajes anteriores.");
    }

    private sealed class SoloLecturaApi(IApiClient api) : IApiClient
    {
        public Task<T?> GetAsync<T>(string endpoint) => api.GetAsync<T>(endpoint);
        public Task<T?> PostAsync<T>(string endpoint, object body) => throw new Exception("Escritura API bloqueada en validación real.");
        public Task<T?> PutAsync<T>(string endpoint, object body) => throw new Exception("Escritura API bloqueada en validación real.");
        public Task<bool> DeleteAsync(string endpoint) => throw new Exception("Escritura API bloqueada en validación real.");
    }
}
