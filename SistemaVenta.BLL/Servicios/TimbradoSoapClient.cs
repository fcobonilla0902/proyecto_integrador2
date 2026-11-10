using System;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SistemaVenta.BLL.Servicios
{
    public interface ITimbradoSoapClient
    {
        Task<byte[]> TimbrarF(string usuario, string password, string xmlComprobante);
    }

    public class TimbradoSoapClient : ITimbradoSoapClient
    {
        private readonly string _url = "https://ws.urbansa.com/app/timbrado.asmx";
        private readonly string _namespace = "http://ws.urbansa.com/";
        private readonly string _soapAction = "http://ws.urbansa.com/TimbrarF";

        public async Task<byte[]> TimbrarF(string usuario, string password, string xmlComprobante)
        {
            HttpClient httpClient = null;

            try
            {
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    ServerCertificateCustomValidationCallback = ValidarCertificadoPac

                };

                httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromMinutes(5)
                };

                string soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <TimbrarF xmlns=""{_namespace}"">
            <Usuario>{EscapeXml(usuario)}</Usuario>
            <Password>{EscapeXml(password)}</Password>
            <StrXml>{EscapeXml(xmlComprobante)}</StrXml>
        </TimbrarF>
    </soap:Body>
</soap:Envelope>";

                using var request = new HttpRequestMessage(HttpMethod.Post, _url);
                request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                request.Headers.Add("SOAPAction", $"\"{_soapAction}\"");

                var response = await httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                XDocument doc = XDocument.Parse(responseContent);
                XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace ns = _namespace;

                var faultElement = doc.Descendants(soap + "Fault").FirstOrDefault();
                if (faultElement != null)
                {
                    string faultCode = faultElement.Element("faultcode")?.Value ?? "";
                    string faultString = faultElement.Element("faultstring")?.Value ?? "Error desconocido";

                    var detailElement = faultElement.Element("detail");
                    string errorNumber = "";
                    string errorMessage = "";

                    if (detailElement != null)
                    {
                        var errorElement = detailElement.Descendants()
                            .FirstOrDefault(e => e.Name.LocalName == "Error");

                        if (errorElement != null)
                        {
                            errorNumber = errorElement.Elements()
                                .FirstOrDefault(e => e.Name.LocalName == "ErrorNumber")?.Value ?? "";

                            errorMessage = errorElement.Elements()
                                .FirstOrDefault(e => e.Name.LocalName == "ErrorMessage")?.Value ?? "";
                        }
                    }

                    if (errorNumber == "1251")
                    {
                        throw new FolioDuplicadoException(
                            $"Este folio ya fue timbrado anteriormente. Detalle: {errorMessage}"
                        );
                    }

                    if (errorNumber.Trim() == "5125")
                    {
                        throw new SoapException(
                            "El PAC reportó que el servicio del SAT no responde (error 5125). " +
                            "No se pudo confirmar el timbrado. Consulte al soporte del PAC con el folio de la venta " +
                            "para verificar si existe un UUID antes de reintentar. El intento se conserva pendiente para evitar duplicados.");
                    }

                    throw new SoapException($"{faultString}\nError #{errorNumber}: {errorMessage}");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Error HTTP {response.StatusCode}: {response.ReasonPhrase}\n" +
                        $"Respuesta: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}"
                    );
                }

                var resultElement = doc.Descendants(ns + "TimbrarFResult").FirstOrDefault()
                                   ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "TimbrarFResult");

                if (resultElement == null || string.IsNullOrWhiteSpace(resultElement.Value))
                    throw new Exception("El servicio no devolvió TimbrarFResult");

                return Convert.FromBase64String(resultElement.Value);
            }
            catch (FolioDuplicadoException)
            {
                throw;
            }
            catch (SoapException)
            {
                throw;
            }
            catch (HttpRequestException httpEx)
            {
                if (httpEx.HttpRequestError == HttpRequestError.SecureConnectionError)
                    throw new ConexionSeguraPacException(
                        $"No se pudo establecer la conexión SSL con el PAC; el comprobante no se envió. Detalle: {httpEx.GetBaseException().Message}. Verifique la fecha del servidor y el certificado HTTPS del proveedor.", httpEx);
                var causa = httpEx.GetBaseException();
                var detalle = causa == httpEx ? httpEx.Message : $"{httpEx.Message} Detalle: {causa.Message}";
                throw new Exception($"Error de conexión con {_url}: {detalle}", httpEx);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("El servicio tardó demasiado en responder");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al llamar al servicio de timbrado: {ex.Message}", ex);
            }
            finally
            {
                httpClient?.Dispose();
            }
        }

        private static bool ValidarCertificadoPac(HttpRequestMessage request,
            X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;

            // Compatibilidad con el certificado vencido del PAC; no aceptar otros errores TLS.
            return request.RequestUri?.Scheme == Uri.UriSchemeHttps
                && string.Equals(request.RequestUri.Host, "ws.urbansa.com", StringComparison.OrdinalIgnoreCase)
                && certificate != null
                && errors == SslPolicyErrors.RemoteCertificateChainErrors
                && chain != null
                && chain.ChainStatus.Length > 0
                && chain.ChainStatus.All(status => status.Status == X509ChainStatusFlags.NotTimeValid);
        }

        private string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }

    public class ConexionSeguraPacException : Exception
    {
        public ConexionSeguraPacException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class SoapException : Exception
    {
        public SoapException(string message) : base(message) { }
        public SoapException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class FolioDuplicadoException : Exception
    {
        public FolioDuplicadoException(string message) : base(message) { }
        public FolioDuplicadoException(string message, Exception innerException) : base(message, innerException) { }
    }
}
