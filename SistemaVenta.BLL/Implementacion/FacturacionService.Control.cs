using SistemaVenta.Entity.Models;
using System.Text.Json;
using System.Xml.Linq;

namespace SistemaVenta.BLL.Implementacion;

public partial class FacturacionService
{
    public async Task<string> ValidarVenta(Venta venta, Cliente cliente, Negocio negocio)
    {
        if (venta.IdVenta <= 0 || string.IsNullOrWhiteSpace(venta.NumeroVenta) ||
            venta.NumeroVenta.Any(c => !char.IsAsciiDigit(c)))
            throw new InvalidOperationException("La venta debe tener ID y folio numérico válidos.");
        if (!string.IsNullOrWhiteSpace(venta.UUID))
            throw new InvalidOperationException("La venta ya tiene UUID.");
        if (!ValidarDatosFiscalesCliente(cliente))
            throw new InvalidOperationException("El cliente no tiene datos fiscales completos.");
        if (negocio == null || new[] { negocio.Rfc, negocio.Nombre, negocio.CodigoPostal, negocio.RegimenFiscal }.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Faltan datos fiscales del emisor.");
        if (new[] { venta.ClaveFormaPago, venta.ClaveMetodoPago, venta.ClaveUsoCFDI }.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Faltan claves de los catálogos de pago o uso CFDI.");
        if (venta.DetalleVenta.Count == 0 || venta.DetalleVenta.Any(d => d.Cantidad is null or <= 0 || d.Precio is null or < 0 ||
            string.IsNullOrWhiteSpace(d.ClaveProdServSat) || string.IsNullOrWhiteSpace(d.ClaveUnidadSat)))
            throw new InvalidOperationException("Hay conceptos sin cantidad, precio o claves SAT válidas.");
        foreach (var d in venta.DetalleVenta)
        {
            if (d.ValorImpuesto is null or < 0 or > 1 || d.ObjetoImpuesto is not ("01" or "02") ||
                (d.ObjetoImpuesto == "02" && (d.FactorImpuesto != "Tasa" || string.IsNullOrWhiteSpace(d.TipoImpuesto))))
                throw new InvalidOperationException("La configuración de impuestos no está soportada por este flujo SOAP.");
            decimal importe = Math.Round(d.Cantidad!.Value * d.Precio!.Value, 2, MidpointRounding.AwayFromZero);
            decimal descuento = d.DescuentoDinero ?? 0m;
            if (descuento < 0 || descuento > importe || (d.SubTotal.HasValue && d.SubTotal != importe))
                throw new InvalidOperationException("El importe o descuento del concepto no coincide con la venta.");
            decimal esperado = d.ObjetoImpuesto == "02"
                ? Math.Round((importe - descuento) * d.ValorImpuesto.Value, 2, MidpointRounding.AwayFromZero) : 0m;
            if (!d.ImpuestoDinero.HasValue || d.ImpuestoDinero.Value != esperado)
                throw new InvalidOperationException("El impuesto del concepto no coincide con su base y tasa.");
        }
        string xml = await ConstruirXMLComprobante(venta, cliente, negocio);
        var comprobante = XDocument.Parse(xml);
        decimal total = decimal.Parse(comprobante.Root!.Element("total")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        if (!venta.Total.HasValue || total != venta.Total.Value)
            throw new InvalidOperationException("El total del comprobante no coincide con el total de la venta.");
        return xml;
    }

    public async Task<(bool exito, string rutaPDF, string rutaXML, string uuid, string mensaje)> TimbrarVenta(
        Venta venta, Cliente cliente, Negocio negocio, string rutaGuardado)
    {
        try
        {
            await ValidarVenta(venta, cliente, negocio);
            Directory.CreateDirectory(rutaGuardado);
            string prefijo = Path.Combine(rutaGuardado, $"Factura_{venta.NumeroVenta}_");
            // FileShare.None también protege cuando hay varios procesos en el mismo servidor.
            using var bloqueo = new FileStream(prefijo + "lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            string recibo = prefijo + "resultado.json";
            if (File.Exists(recibo))
            {
                var anterior = JsonSerializer.Deserialize<ResultadoPersistido>(await File.ReadAllTextAsync(recibo))!;
                if (anterior.IdVenta != venta.IdVenta || anterior.RfcEmisor != negocio.Rfc)
                    throw new InvalidOperationException("El folio ya pertenece a otra venta o emisor. Revise el correlativo antes de continuar.");
                if (!File.Exists(anterior.Pdf) || !File.Exists(anterior.Xml))
                    throw new InvalidOperationException("Faltan archivos de una factura existente; restaure el respaldo sin volver a timbrar.");
                return (true, anterior.Pdf, anterior.Xml, anterior.Uuid, "Factura recuperada; pendiente de sincronizar por API.");
            }
            string pendiente = prefijo + "pendiente";
            string zip = prefijo + "pac.zip";
            if (File.Exists(pendiente))
            {
                var intento = JsonSerializer.Deserialize<IntentoPersistido>(await File.ReadAllTextAsync(pendiente))!;
                if (intento.IdVenta != venta.IdVenta || intento.RfcEmisor != negocio.Rfc)
                    throw new InvalidOperationException("El folio tiene un intento de otra venta o emisor. Requiere conciliación.");
            }
            if (File.Exists(pendiente) && !File.Exists(zip))
                throw new InvalidOperationException("Existe un intento pendiente. Consulte al PAC antes de volver a timbrar.");
            if (!File.Exists(pendiente) && Directory.EnumerateFiles(rutaGuardado, $"Factura_{venta.NumeroVenta}_*.xml").Any())
                throw new InvalidOperationException("Ya existen archivos de esta factura. Concilie su UUID sin volver a timbrar.");
            if (!File.Exists(zip) && !string.Equals(_configuracion["Facturacion:TimbradoHabilitado"], "true", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Timbrado deshabilitado. Ejecute primero la validación previa.");
            if (string.IsNullOrWhiteSpace(negocio.UsuarioPac) || string.IsNullOrWhiteSpace(negocio.PasswordPac))
                throw new InvalidOperationException("Configure las credenciales PAC mediante User Secrets.");
            await File.WriteAllTextAsync(pendiente, JsonSerializer.Serialize(new IntentoPersistido(venta.IdVenta, negocio.Rfc!, DateTimeOffset.UtcNow)));
            (bool exito, string rutaPDF, string rutaXML, string uuid, string mensaje) resultado;
            try
            {
                resultado = await TimbrarVentaCore(venta, cliente, negocio, rutaGuardado);
            }
            catch (SistemaVenta.BLL.Servicios.ConexionSeguraPacException)
            {
                // El handshake TLS falla antes de enviar el comprobante. No hay resultado incierto.
                File.Delete(pendiente);
                throw;
            }
            if (resultado.exito)
                await File.WriteAllTextAsync(recibo, JsonSerializer.Serialize(new ResultadoPersistido(venta.IdVenta, negocio.Rfc!, resultado.rutaPDF, resultado.rutaXML, resultado.uuid)));
            return resultado;
        }
        catch (Exception ex)
        {
            return (false, null!, null!, null!, ex.Message);
        }
    }

    private sealed record IntentoPersistido(int IdVenta, string RfcEmisor, DateTimeOffset Fecha);
    private sealed record ResultadoPersistido(int IdVenta, string RfcEmisor, string Pdf, string Xml, string Uuid);
}
