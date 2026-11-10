using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.BLL.Servicios;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity.Models;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace SistemaVenta.BLL.Implementacion
{
    public partial class FacturacionService : IFacturacionService
    {
        private readonly INegocioService _negocioService;
        private readonly ITimbradoSoapClient _timbrado;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuracion;

        public FacturacionService(
            INegocioService negocioService,
            Microsoft.Extensions.Configuration.IConfiguration configuracion,
            ITimbradoSoapClient timbrado
            ) //  AGREGAR
        {
            
            _negocioService = negocioService;
            _timbrado = timbrado;
            _configuracion = configuracion;
            
        }

        public bool ValidarDatosFiscalesCliente(Cliente cliente)
        {
            if (cliente == null) return false;

            return !string.IsNullOrWhiteSpace(cliente.Rfc) &&
                   !string.IsNullOrWhiteSpace(cliente.DomicilioFiscalReceptor) &&
                   !string.IsNullOrWhiteSpace(cliente.RegimenFiscalReceptor) &&
                   !string.IsNullOrWhiteSpace(cliente.Nombre);
        }

        public async Task<bool> ValidarConfiguracionNegocio()
        {
            try
            {
                var negocio = await _negocioService.Obtener();

                if (negocio == null)
                    return false;

                bool datosBasicos = !string.IsNullOrWhiteSpace(negocio.Rfc) &&
                                   !string.IsNullOrWhiteSpace(negocio.Nombre) &&
                                   !string.IsNullOrWhiteSpace(negocio.RegimenFiscal) &&
                                   !string.IsNullOrWhiteSpace(negocio.CodigoPostal);

                bool credencialesPAC = !string.IsNullOrWhiteSpace(negocio.UsuarioPac) &&
                                       !string.IsNullOrWhiteSpace(negocio.PasswordPac);

                return datosBasicos && credencialesPAC;
            }
            catch
            {
                return false;
            }
        }

       
        public async Task<(bool exito, string rutaPDF, string rutaXML, string uuid, string mensaje)> TimbrarVentaCore(
    Venta venta,
    Cliente cliente,
    Negocio negocio,
    string rutaGuardado)
        {
            try
            {
                Console.WriteLine("[TIMBRAR] ENTRE A TIMBRARVENTA NUEVO");

                if (negocio == null)
                {
                    return (false, null, null, null, "No se encontró la configuración del negocio");
                }

                if (string.IsNullOrWhiteSpace(negocio.Rfc) ||
                    string.IsNullOrWhiteSpace(negocio.Nombre) ||
                    string.IsNullOrWhiteSpace(negocio.RegimenFiscal) ||
                    string.IsNullOrWhiteSpace(negocio.CodigoPostal))
                {
                    return (false, null, null, null,
                        "El negocio no tiene configurados todos los datos fiscales requeridos");
                }

                if (!ValidarDatosFiscalesCliente(cliente))
                {
                    return (false, null, null, null, "El cliente no tiene datos fiscales completos");
                }

                string usuarioPAC = negocio.UsuarioPac;
                string passwordPAC = negocio.PasswordPac;

                if (string.IsNullOrWhiteSpace(usuarioPAC) || string.IsNullOrWhiteSpace(passwordPAC))
                {
                    return (false, null, null, null, "No se han configurado las credenciales del PAC");
                }

                string xmlComprobante = await ConstruirXMLComprobante(venta, cliente, negocio);

                ITimbradoSoapClient timbradoService = _timbrado;
                string respaldo = Path.Combine(rutaGuardado, $"Factura_{venta.NumeroVenta}_pac.zip");
                byte[] zipBytes;
                if (File.Exists(respaldo))
                    zipBytes = await File.ReadAllBytesAsync(respaldo);
                else
                {
                    byte[] respuestaPac = await timbradoService.TimbrarF(usuarioPAC, passwordPAC, xmlComprobante);
                    await File.WriteAllBytesAsync(respaldo, respuestaPac);
                    zipBytes = respuestaPac;
                }

                if (zipBytes == null || zipBytes.Length == 0)
                {
                    return (false, null, null, null, "El servicio de timbrado no devolvió datos");
                }

                Console.WriteLine($"[TIMBRAR] ZIP recibido. Bytes: {zipBytes.Length}");

                var (pdfBytes, xmlBytes, uuid) = ExtraerArchivosDelZIP(zipBytes);

                Console.WriteLine($"[TIMBRAR] PDF extraído: {(pdfBytes != null ? pdfBytes.Length : 0)} bytes");
                Console.WriteLine($"[TIMBRAR] XML extraído: {(xmlBytes != null ? xmlBytes.Length : 0)} bytes");
                Console.WriteLine($"[TIMBRAR] UUID extraído: {uuid}");

                if (!Guid.TryParse(uuid, out _)) return (false, null, null, null, "El XML no contiene UUID; requiere conciliación con el PAC.");
                if (pdfBytes == null || pdfBytes.Length < 5 || Encoding.ASCII.GetString(pdfBytes, 0, 5) != "%PDF-")
                {
                    return (false, null, null, null, "No se extrajo el PDF del ZIP timbrado");
                }

                if (xmlBytes == null)
                {
                    return (false, null, null, null, "No se extrajo el XML del ZIP timbrado");
                }

                Directory.CreateDirectory(rutaGuardado);

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string nombreArchivo = $"Factura_{venta.NumeroVenta}_{timestamp}";
                string rutaPDF = Path.Combine(rutaGuardado, $"{nombreArchivo}.pdf");
                string rutaXML = Path.Combine(rutaGuardado, $"{nombreArchivo}.xml");

                Console.WriteLine($"[TIMBRAR] rutaGuardado recibida: {rutaGuardado}");
                Console.WriteLine($"[TIMBRAR] rutaPDF final: {rutaPDF}");
                Console.WriteLine($"[TIMBRAR] rutaXML final: {rutaXML}");

                await File.WriteAllBytesAsync(rutaPDF, pdfBytes);
                await File.WriteAllBytesAsync(rutaXML, xmlBytes);

                bool existePDF = File.Exists(rutaPDF);
                bool existeXML = File.Exists(rutaXML);

                Console.WriteLine($"[TIMBRAR] existe PDF: {existePDF}");
                Console.WriteLine($"[TIMBRAR] existe XML: {existeXML}");

                if (!existePDF || !existeXML)
                {
                    return (false, null, null, null, "Los archivos no quedaron guardados correctamente");
                }

                return (true, rutaPDF, rutaXML, uuid, "Factura timbrada correctamente");
            }
            catch (ConexionSeguraPacException)
            {
                throw;
            }
            catch (FolioDuplicadoException)
            {
                return (false, null, null, null,
                    "Este folio ya fue timbrado anteriormente. La factura ya existe en el SAT. No es necesario volver a timbrarla.");
            }
            catch (SoapException soapEx)
            {
                return (false, null, null, null, $"Error del PAC: {soapEx.Message}");
            }
            catch (Exception ex)
            {
                return (false, null, null, null, $"Error inesperado: {ex.Message}");
            }
        }

        private Task<string> ConstruirXMLComprobante(Venta venta, Cliente cliente, Negocio negocio)
        {
            string formaPagoXml = !string.IsNullOrWhiteSpace(venta.ClaveFormaPago)
                ? venta.ClaveFormaPago.Trim()
                : "01";

            string metodoPagoXml = !string.IsNullOrWhiteSpace(venta.ClaveMetodoPago)
                ? venta.ClaveMetodoPago.Trim()
                : "PUE";

            string usoCfdiXml = !string.IsNullOrWhiteSpace(venta.ClaveUsoCFDI)
                ? venta.ClaveUsoCFDI.Trim()
                : "S01";

            // Validaciones de consistencia CFDI
            if (metodoPagoXml == "PPD" && formaPagoXml != "99")
            {
                throw new Exception("Configuración CFDI inválida: cuando MetodoPago es PPD, FormaPago debe ser 99.");
            }

            if (metodoPagoXml == "PUE" && formaPagoXml == "99")
            {
                throw new Exception("Configuración CFDI inválida: cuando MetodoPago es PUE, FormaPago no puede ser 99.");
            }

            if (usoCfdiXml == "CP01")
            {
                throw new Exception("Configuración CFDI inválida: UsoCFDI CP01 solo aplica para complemento de pagos, no para factura de ingreso.");
            }

            Console.WriteLine("=========== DEBUG DATOS CFDI ===========");
            Console.WriteLine($"venta.IdFormaPago: {venta.IdFormaPago}");
            Console.WriteLine($"formaPagoXml: {formaPagoXml}");
            Console.WriteLine($"venta.IdMetodoPago: {venta.IdMetodoPago}");
            Console.WriteLine($"metodoPagoXml: {metodoPagoXml}");
            Console.WriteLine($"venta.IdUsoCFDI: {venta.IdUsoCFDI}");
            Console.WriteLine($"usoCfdiXml: {usoCfdiXml}");
            Console.WriteLine("========================================");

            StringBuilder xml = new StringBuilder();

            decimal totalDescuentos = 0m;
            decimal subtotalSinDescuento = 0m;
            decimal totalImpuestosTrasladados = 0m;
            decimal totalImpuestosRetenidos = 0m;

            var conceptosXml = new StringBuilder();
            var trasladosAgrupados = new System.Collections.Generic.Dictionary<(string Impuesto, string Factor, decimal Tasa), (decimal Base, decimal Importe)>();
            var retencionesGlobalesXml = new StringBuilder();

            foreach (var detalle in venta.DetalleVenta)
            {
                string descripcion = detalle.DescripcionProducto?.Trim()
                    ?? throw new InvalidOperationException("Falta descripción del producto.");
                string claveProdServ = detalle.ClaveProdServSat!;
                string claveUnidad = detalle.ClaveUnidadSat!;
                string unidad = detalle.Unidad ?? "PZA";
                string objetoImp = detalle.ObjetoImpuesto!;
                string impuesto = detalle.TipoImpuesto ?? "";
                string tipoFactor = detalle.FactorImpuesto ?? "";
                decimal tasaOCuota = detalle.ValorImpuesto
                    ?? throw new InvalidOperationException("Falta tasa del impuesto.");
                decimal cantidad = detalle.Cantidad ?? 0m;
                decimal valorUnitario = detalle.Precio ?? 0m;

                decimal importeSinDescuento = detalle.SubTotal.HasValue && detalle.SubTotal.Value > 0
                    ? Math.Round(detalle.SubTotal.Value, 2, MidpointRounding.AwayFromZero)
                    : Math.Round(cantidad * valorUnitario, 2, MidpointRounding.AwayFromZero);

                decimal montoDescuento = detalle.DescuentoDinero.HasValue && detalle.DescuentoDinero.Value > 0
                    ? Math.Round(detalle.DescuentoDinero.Value, 2, MidpointRounding.AwayFromZero)
                    : 0m;

                decimal baseImpuesto = Math.Round(importeSinDescuento - montoDescuento, 2, MidpointRounding.AwayFromZero);

                decimal impuestoConcepto = detalle.ImpuestoDinero.HasValue
                    ? Math.Round(detalle.ImpuestoDinero.Value, 2, MidpointRounding.AwayFromZero)
                    : Math.Round(baseImpuesto * tasaOCuota, 2, MidpointRounding.AwayFromZero);

                subtotalSinDescuento += importeSinDescuento;
                totalDescuentos += montoDescuento;

                conceptosXml.Append("<Concepto>");
                conceptosXml.AppendFormat("<claveProdServ>{0}</claveProdServ>", SecurityElement.Escape(claveProdServ));
                conceptosXml.AppendFormat("<noIdentificacion>{0}</noIdentificacion>", detalle.IdProducto?.ToString() ?? "");
                conceptosXml.AppendFormat("<cantidad>{0}</cantidad>", cantidad.ToString("F2", CultureInfo.InvariantCulture));
                conceptosXml.AppendFormat("<claveUnidad>{0}</claveUnidad>", SecurityElement.Escape(claveUnidad));
                conceptosXml.AppendFormat("<unidad>{0}</unidad>", SecurityElement.Escape(unidad));
                conceptosXml.AppendFormat("<descripcion>{0}</descripcion>", SecurityElement.Escape(descripcion));
                conceptosXml.AppendFormat("<valorUnitario>{0}</valorUnitario>", valorUnitario.ToString("F2", CultureInfo.InvariantCulture));
                conceptosXml.AppendFormat("<importe>{0}</importe>", importeSinDescuento.ToString("F2", CultureInfo.InvariantCulture));

                conceptosXml.AppendFormat("<descuento>{0}</descuento>", montoDescuento.ToString("F2", CultureInfo.InvariantCulture));

                conceptosXml.AppendFormat("<objetoImp>{0}</objetoImp>", SecurityElement.Escape(objetoImp));

                if (objetoImp == "02")
                {
                    conceptosXml.Append("<Traslado>");
                    conceptosXml.AppendFormat("<base>{0}</base>", baseImpuesto.ToString("F2", CultureInfo.InvariantCulture));
                    conceptosXml.AppendFormat("<impuesto>{0}</impuesto>", SecurityElement.Escape(impuesto));
                    conceptosXml.AppendFormat("<tipoFactor>{0}</tipoFactor>", SecurityElement.Escape(tipoFactor));
                    conceptosXml.AppendFormat("<tasaOCuota>{0}</tasaOCuota>", tasaOCuota.ToString("F6", CultureInfo.InvariantCulture));
                    conceptosXml.AppendFormat("<importe>{0}</importe>", impuestoConcepto.ToString("F2", CultureInfo.InvariantCulture));
                    conceptosXml.Append("</Traslado>");

                    totalImpuestosTrasladados += impuestoConcepto;

                    var claveTraslado = (impuesto, tipoFactor, tasaOCuota);
                    trasladosAgrupados.TryGetValue(claveTraslado, out var acumulado);
                    trasladosAgrupados[claveTraslado] = (acumulado.Base + baseImpuesto, acumulado.Importe + impuestoConcepto);
                }

                conceptosXml.Append("<rfcACuentaTerceros></rfcACuentaTerceros>");
                conceptosXml.Append("<nombreACuentaTerceros></nombreACuentaTerceros>");
                conceptosXml.Append("<regimenFiscalACuentaTerceros></regimenFiscalACuentaTerceros>");
                conceptosXml.Append("<domicilioFiscalACuentaTerceros></domicilioFiscalACuentaTerceros>");
                conceptosXml.Append("<numeroPedimento></numeroPedimento><cuentaPredial></cuentaPredial>");
                conceptosXml.Append("</Concepto>");
            }

            decimal subTotalComprobante = Math.Round(subtotalSinDescuento, 2, MidpointRounding.AwayFromZero);
            decimal descuentoComprobante = Math.Round(totalDescuentos, 2, MidpointRounding.AwayFromZero);
            decimal impuestosTrasladadosComprobante = Math.Round(totalImpuestosTrasladados, 2, MidpointRounding.AwayFromZero);
            decimal impuestosRetenidosComprobante = Math.Round(totalImpuestosRetenidos, 2, MidpointRounding.AwayFromZero);
            decimal totalComprobante = Math.Round(
                subTotalComprobante - descuentoComprobante + impuestosTrasladadosComprobante - impuestosRetenidosComprobante,
                2,
                MidpointRounding.AwayFromZero
            );

            xml.Append("<Comprobante>");
            xml.AppendFormat("<idLocal>{0}</idLocal>", venta.NumeroVenta ?? "");
            xml.Append("<version>4.0</version>");
            xml.AppendFormat("<serie>{0}</serie>", SecurityElement.Escape(negocio.SerieFactura ?? ""));
            xml.AppendFormat("<folio>{0}</folio>", SecurityElement.Escape(venta.NumeroVenta ?? ""));
            xml.AppendFormat("<formaPago>{0}</formaPago>", SecurityElement.Escape(formaPagoXml));
            xml.Append("<condicionesDePago></condicionesDePago>");
            xml.AppendFormat("<subTotal>{0}</subTotal>", subTotalComprobante.ToString("F2", CultureInfo.InvariantCulture));
            xml.AppendFormat("<descuento>{0}</descuento>", descuentoComprobante.ToString("F2", CultureInfo.InvariantCulture));
            xml.Append("<moneda>MXN</moneda>");
            xml.Append("<tipoCambio>1.0</tipoCambio>");
            xml.AppendFormat("<total>{0}</total>", totalComprobante.ToString("F2", CultureInfo.InvariantCulture));
            xml.Append("<tipoDeComprobante>I</tipoDeComprobante>");
            xml.Append("<exportacion>01</exportacion>");
            xml.AppendFormat("<metodoPago>{0}</metodoPago>", SecurityElement.Escape(metodoPagoXml));
            xml.AppendFormat("<lugarExpedicion>{0}</lugarExpedicion>", SecurityElement.Escape(negocio.CodigoPostal ?? ""));
            xml.Append("<confirmacion></confirmacion>");
            xml.Append("<periodicidad></periodicidad><meses></meses><ano></ano>");
            xml.Append("<Relacionado><tipoRelacion></tipoRelacion><uUID></uUID></Relacionado>");
            xml.AppendFormat("<regimenFiscal>{0}</regimenFiscal>", SecurityElement.Escape(negocio.RegimenFiscal ?? ""));
            xml.Append("<facAtrAdquirente></facAtrAdquirente>");
            xml.AppendFormat("<rfc>{0}</rfc>", SecurityElement.Escape(cliente.Rfc ?? ""));
            xml.AppendFormat("<nombre>{0}</nombre>", SecurityElement.Escape(cliente.Nombre ?? ""));
            xml.AppendFormat("<domicilioFiscalReceptor>{0}</domicilioFiscalReceptor>", SecurityElement.Escape(cliente.DomicilioFiscalReceptor ?? ""));
            xml.Append("<residenciaFiscal></residenciaFiscal>");
            xml.Append("<numRegIdTrib></numRegIdTrib>");
            xml.AppendFormat("<regimenFiscalReceptor>{0}</regimenFiscalReceptor>", SecurityElement.Escape(cliente.RegimenFiscalReceptor ?? ""));
            xml.AppendFormat("<usoCFDI>{0}</usoCFDI>", SecurityElement.Escape(usoCfdiXml));
            xml.Append(conceptosXml.ToString());

            if (totalImpuestosRetenidos == 0)
                xml.Append("<totalImpuestosRetenidos></totalImpuestosRetenidos>");

            if (trasladosAgrupados.Count > 0)
            {
                xml.AppendFormat("<totalImpuestosTrasladados>{0}</totalImpuestosTrasladados>",
                    impuestosTrasladadosComprobante.ToString("F2", CultureInfo.InvariantCulture));
                foreach (var traslado in trasladosAgrupados)
                {
                    xml.Append("<Traslados>");
                    xml.AppendFormat("<base>{0}</base>", traslado.Value.Base.ToString("F2", CultureInfo.InvariantCulture));
                    xml.AppendFormat("<impuesto>{0}</impuesto>", SecurityElement.Escape(traslado.Key.Impuesto));
                    xml.AppendFormat("<tipoFactor>{0}</tipoFactor>", SecurityElement.Escape(traslado.Key.Factor));
                    xml.AppendFormat("<tasaOCuota>{0}</tasaOCuota>", traslado.Key.Tasa.ToString("F6", CultureInfo.InvariantCulture));
                    xml.AppendFormat("<importe>{0}</importe>", traslado.Value.Importe.ToString("F2", CultureInfo.InvariantCulture));
                    xml.Append("</Traslados>");
                }
            }

            if (totalImpuestosRetenidos > 0)
            {
                xml.AppendFormat("<totalImpuestosRetenidos>{0}</totalImpuestosRetenidos>",
                    impuestosRetenidosComprobante.ToString("F2", CultureInfo.InvariantCulture));
                xml.Append("<Retenciones>");
                xml.Append(retencionesGlobalesXml.ToString());
                xml.Append("</Retenciones>");
            }

            xml.Append("</Comprobante>");

            string xmlCompleto = xml.ToString();



            return Task.FromResult(xmlCompleto);
        }






        private (byte[] pdf, byte[] xml, string uuid) ExtraerArchivosDelZIP(byte[] zipBytes)
        {
            byte[] pdfBytes = null;
            byte[] xmlBytes = null;
            string uuid = null;

            using (MemoryStream zipStream = new MemoryStream(zipBytes))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                Console.WriteLine($"[ZIP] Total de entradas: {archive.Entries.Count}");

                foreach (var entry in archive.Entries)
                {
                    Console.WriteLine($"[ZIP] Entry: {entry.FullName} | Tamaño: {entry.Length}");
                }

                var pdfEntry = archive.Entries.FirstOrDefault(e =>
                    e.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

                if (pdfEntry != null)
                {
                    Console.WriteLine($"[ZIP] PDF encontrado: {pdfEntry.FullName}");

                    using (Stream pdfStream = pdfEntry.Open())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        pdfStream.CopyTo(ms);
                        pdfBytes = ms.ToArray();
                    }
                }
                else
                {
                    Console.WriteLine("[ZIP] No se encontró ningún PDF dentro del ZIP");
                }

                var xmlEntry = archive.Entries.FirstOrDefault(e =>
                    e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

                if (xmlEntry != null)
                {
                    Console.WriteLine($"[ZIP] XML encontrado: {xmlEntry.FullName}");

                    using (Stream xmlStream = xmlEntry.Open())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        xmlStream.CopyTo(ms);
                        xmlBytes = ms.ToArray();

                        try
                        {
                            ms.Position = 0;
                            XDocument doc = XDocument.Load(ms);

                            var timbreFiscal = doc.Descendants()
                                .FirstOrDefault(e => e.Name.LocalName == "TimbreFiscalDigital");

                            uuid = timbreFiscal?.Attribute("UUID")?.Value;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ZIP] No se pudo leer el UUID del XML: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[ZIP] No se encontró ningún XML dentro del ZIP");
                }
            }

            return (pdfBytes, xmlBytes, uuid);
        }
    }
}






