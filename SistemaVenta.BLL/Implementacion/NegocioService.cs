using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SistemaVenta.BLL.Implementacion
{
    public class NegocioService : INegocioService
    {
        private readonly ILocalStorageService _cloudinaryServices;
        private readonly IConfiguration _configuracion;
        private readonly IHttpClientFactory _httpClientFactory;

        public NegocioService(
            ILocalStorageService cloudinaryServices,
            IConfiguration configuracion,
            IHttpClientFactory httpClientFactory)
        {
            _cloudinaryServices = cloudinaryServices;
            _configuracion = configuracion;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Negocio> Obtener()
        {
            using var clienteApi = _httpClientFactory.CreateClient();
            var apiBase = _configuracion["ApiSettings:BaseUrl"]
                ?? throw new InvalidOperationException("Falta ApiSettings:BaseUrl.");
            clienteApi.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
            using var respuestaApi = await clienteApi.GetAsync("api/negocio/1");
            respuestaApi.EnsureSuccessStatusCode();
            Negocio? negocio = JsonSerializer.Deserialize<Negocio>(
                await respuestaApi.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (negocio == null)
                throw new InvalidOperationException("No se encontró el negocio con idNegocio = 1.");

            // Las claves SAT son datos calculados, ignorados por Entity Framework.
            negocio.CodigoPostal = await ObtenerClaveCatalogo(
                "api/codigopostalsat", negocio.IdCodigoPostal, "c_CodigoPostal");
            negocio.RegimenFiscal = await ObtenerClaveCatalogo(
                "api/regimenfiscalsat", negocio.IdRegimenFiscal, "c_RegimenFiscal");

            // Estos valores NO existen en dbo.Negocio: se leen de configuración externa.
            negocio.UsuarioPac = _configuracion["Facturacion:UsuarioPac"];
            negocio.PasswordPac = _configuracion["Facturacion:PasswordPac"];
            negocio.SerieFactura = _configuracion["Facturacion:SerieFactura"];
            negocio.SimboloMoneda = _configuracion["Facturacion:SimboloMoneda"];

            if (int.TryParse(_configuracion["Facturacion:FolioInicialFactura"], out int folio))
                negocio.FolioInicialFactura = folio;

            return negocio;
        }

        private async Task<string?> ObtenerClaveCatalogo(string endpoint, int? id, string propiedad)
        {
            if (!id.HasValue || id.Value <= 0)
                return null;

            string baseUrl = _configuracion["ApiSettings:BaseUrl"]
                ?? throw new InvalidOperationException("Falta ApiSettings:BaseUrl en la configuración.");

            using var cliente = _httpClientFactory.CreateClient();
            cliente.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            using var respuesta = await cliente.GetAsync($"{endpoint}/{id.Value}");
            respuesta.EnsureSuccessStatusCode();

            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            if (json.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"Respuesta inesperada del catálogo {endpoint}.");

            foreach (var campo in json.RootElement.EnumerateObject())
            {
                if (string.Equals(campo.Name, propiedad, StringComparison.OrdinalIgnoreCase))
                    return campo.Value.ValueKind == JsonValueKind.Null ? null : campo.Value.ToString();
            }

            throw new InvalidOperationException(
                $"El catálogo {endpoint}/{id.Value} no devolvió el campo {propiedad}.");
        }

        public async Task<Negocio> GuardarCambios(Negocio entidad, Stream Logo = null, string NombreLogo = "")
        {
            using var clienteApi = _httpClientFactory.CreateClient();
            var apiBase = _configuracion["ApiSettings:BaseUrl"]
                ?? throw new InvalidOperationException("Falta ApiSettings:BaseUrl.");
            clienteApi.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
            using var respuestaApi = await clienteApi.GetAsync("api/negocio/1");
            respuestaApi.EnsureSuccessStatusCode();
            Negocio? negocio = JsonSerializer.Deserialize<Negocio>(
                await respuestaApi.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (negocio == null)
                throw new InvalidOperationException("No se encontró el negocio con idNegocio = 1.");

            negocio.Rfc = entidad.Rfc;
            negocio.Nombre = entidad.Nombre;
            negocio.Correo = entidad.Correo;
            negocio.Direccion = entidad.Direccion;
            negocio.Telefono = entidad.Telefono;

            // Las claves fiscales de texto y credenciales del PAC no se guardan en dbo.Negocio.
            // Los IDs de catálogos solo se actualizan cuando se proporcionan explícitamente.
            if (entidad.IdCodigoPostal.HasValue)
                negocio.IdCodigoPostal = entidad.IdCodigoPostal;
            if (entidad.IdRegimenFiscal.HasValue)
                negocio.IdRegimenFiscal = entidad.IdRegimenFiscal;
            if (entidad.IdPais.HasValue)
                negocio.IdPais = entidad.IdPais;
            if (entidad.IdEstado.HasValue)
                negocio.IdEstado = entidad.IdEstado;
            if (entidad.IdMunicipio.HasValue)
                negocio.IdMunicipio = entidad.IdMunicipio;

            if (string.IsNullOrWhiteSpace(negocio.NombreLogo))
                negocio.NombreLogo = NombreLogo;

            if (Logo != null && Logo.Length > 0)
            {
                string nombreLogo = Guid.NewGuid().ToString("N") + ".JPG";
                string urlLogo = await _cloudinaryServices.SubirStorage(
                    Logo, "carpeta_logo", nombreLogo);
                negocio.NombreLogo = nombreLogo;
                negocio.UrlLogo = urlLogo;
            }

            using var actualizacion = await clienteApi.PutAsJsonAsync("api/negocio", new
            {
                negocio.IdNegocio, negocio.UrlLogo, negocio.NombreLogo, negocio.Rfc,
                negocio.Nombre, negocio.Correo, negocio.Direccion, negocio.Telefono,
                negocio.IdCodigoPostal, negocio.IdPais, negocio.IdEstado,
                negocio.IdMunicipio, negocio.IdRegimenFiscal
            });
            actualizacion.EnsureSuccessStatusCode();
            return negocio;
        }
    }
}


