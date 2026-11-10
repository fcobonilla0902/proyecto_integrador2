using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SistemaVenta.AplicacionWeb.Services.Api
{
    public interface IApiClient
    {
        Task<T?> GetAsync<T>(string endpoint);
        Task<T?> PostAsync<T>(string endpoint, object body);
        Task<T?> PutAsync<T>(string endpoint, object body);
        Task<bool> DeleteAsync(string endpoint);
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _http.GetAsync(endpoint);
                await ValidarRespuesta(response);
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }
            catch (HttpRequestException)
            {
                throw new Exception("No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("La API tardó demasiado en responder. Verifica que esté encendida y funcionando.");
            }
        }

        public async Task<T?> PostAsync<T>(string endpoint, object body)
        {
            try
            {
                var content = BuildJsonContent(body);
                var response = await _http.PostAsync(endpoint, content);
                await ValidarRespuesta(response);
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }
            catch (HttpRequestException)
            {
                throw new Exception("No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("La API tardó demasiado en responder. Verifica que esté encendida y funcionando.");
            }
        }

        public async Task<T?> PutAsync<T>(string endpoint, object body)
        {
            try
            {
                var content = BuildJsonContent(body);
                var response = await _http.PutAsync(endpoint, content);
                await ValidarRespuesta(response);
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }
            catch (HttpRequestException)
            {
                throw new Exception("No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("La API tardó demasiado en responder. Verifica que esté encendida y funcionando.");
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _http.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                throw new Exception("No se pudo conectar con la API. Verifica que API_Equipo1 esté ejecutándose.");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("La API tardó demasiado en responder. Verifica que esté encendida y funcionando.");
            }
        }

        private static async Task ValidarRespuesta(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            string detalle = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(detalle))
            {
                throw new Exception($"La API respondió con error HTTP {(int)response.StatusCode}.");
            }

            throw new Exception($"La API respondió con error HTTP {(int)response.StatusCode}: {detalle}");
        }

        private static StringContent BuildJsonContent(object body)
        {
            var json = JsonSerializer.Serialize(body);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}