using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SistemaVenta.BLL.Implementacion;
using SistemaVenta.BLL.Servicios;
using SistemaVenta.DAL.DBContext;
using SistemaVenta.Entity.Models;

if (args.Contains("--validar-api-real"))
{
    await VerificacionApiReal.Ejecutar();
    return;
}

var carpeta = Path.Combine(Path.GetTempPath(), "SistemaVenta-verificacion-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(carpeta);
var negocio = new Negocio { Rfc = "AAA010101AAA", Nombre = "PRUEBA", CodigoPostal = "01000", RegimenFiscal = "601", UsuarioPac = "simulado", PasswordPac = "simulado" };
var cliente = new Cliente { Rfc = "XAXX010101000", Nombre = "PUBLICO", DomicilioFiscalReceptor = "01000", RegimenFiscalReceptor = "616" };
Venta VentaPrueba(string folio, decimal tasa = .16m) => new()
{
    IdVenta = int.Parse(folio), NumeroVenta = folio, ClaveFormaPago = "01", ClaveMetodoPago = "PUE", ClaveUsoCFDI = "S01",
    Total = 100 * (1 + tasa), SubTotal = 100, ImpuestoTotal = 100 * tasa,
    DetalleVenta = new List<DetalleVenta> { new() { IdProducto = 1, DescripcionProducto = "PRUEBA & XML", Cantidad = 1, Precio = 100,
        SubTotal = 100, DescuentoDinero = 0, ImpuestoDinero = 100 * tasa, ValorImpuesto = tasa,
        ClaveProdServSat = "01010101", ClaveUnidadSat = "H87", ObjetoImpuesto = "02", TipoImpuesto = "002", FactorImpuesto = "Tasa" } }
};
FacturacionService Servicio(PacSimulado pac, bool habilitado = true) => new(null!,
    new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Facturacion:TimbradoHabilitado"] = habilitado.ToString() }).Build(), pac);
void Verificar(bool condicion, string caso)
{
    if (!condicion) throw new Exception("FALLO: " + caso);
    Console.WriteLine("PASS: " + caso);
}

// Construir el modelo no abre conexiones ni ejecuta comandos SQL.
using (var db = new DbventaContext(new DbContextOptionsBuilder<DbventaContext>().UseSqlServer("Server=invalid;Database=invalid;Integrated Security=true;TrustServerCertificate=true").Options))
{
    Verificar(db.Model.FindEntityType(typeof(Venta)) != null, "modelo EF reconstruido sin conectar SQL");
    Verificar(db.Model.FindEntityType(typeof(Negocio))!.FindProperty("PasswordPac") == null, "credenciales PAC fuera del modelo SQL");
}
var pac = new PacSimulado();
var servicio = Servicio(pac);
string xml = await servicio.ValidarVenta(VentaPrueba("100001"), cliente, negocio);
Verificar(xml.Contains("PRUEBA &amp; XML") && pac.Llamadas == 0, "prevalidación XML sin llamar al PAC");
string cero = await servicio.ValidarVenta(VentaPrueba("100002", 0), cliente, negocio);
var formatoPac = System.Xml.Linq.XDocument.Parse(xml).Root!;
Verificar(formatoPac.Element("fecha") == null && formatoPac.Element("email") == null
    && formatoPac.Element("Relacionado")!.Element("tipoRelacion") != null
    && formatoPac.Element("facAtrAdquirente") != null
    && formatoPac.Element("totalImpuestosRetenidos") != null
    && formatoPac.Element("Concepto")!.Element("descuento")!.Value == "0.00"
    && formatoPac.Element("Concepto")!.Element("cuentaPredial") != null,
    "formato de entrada compatible con el proyecto de referencia sin fecha local");
var ventaAgrupada = VentaPrueba("100010");
ventaAgrupada.DetalleVenta.Add(VentaPrueba("100011").DetalleVenta.Single());
ventaAgrupada.Total = 232;
ventaAgrupada.SubTotal = 200;
ventaAgrupada.ImpuestoTotal = 32;
var xmlAgrupado = System.Xml.Linq.XDocument.Parse(await servicio.ValidarVenta(ventaAgrupada, cliente, negocio));
var trasladoGlobal = xmlAgrupado.Root!.Elements("Traslados").Single();
Verificar(trasladoGlobal.Element("Traslado") == null && trasladoGlobal.Element("base")!.Value == "200.00" && trasladoGlobal.Element("importe")!.Value == "32.00", "traslado global plano del PAC agrupa conceptos de la misma tasa");
Verificar(cero.Contains("<tasaOCuota>0.000000</tasaOCuota>") && cero.Contains("<total>100.00</total>"), "IVA cero conservado");
var deshabilitado = await Servicio(pac, false).TimbrarVenta(VentaPrueba("100003"), cliente, negocio, carpeta);
Verificar(!deshabilitado.exito && pac.Llamadas == 0, "timbrado deshabilitado por configuración");
var primera = await servicio.TimbrarVenta(VentaPrueba("100004"), cliente, negocio, carpeta);
Verificar(primera.exito && File.Exists(primera.rutaXML) && File.Exists(primera.rutaPDF), "respuesta SOAP simulada genera PDF/XML y UUID");
var segunda = await servicio.TimbrarVenta(VentaPrueba("100004"), cliente, negocio, carpeta);
Verificar(segunda.exito && segunda.uuid == primera.uuid && pac.Llamadas == 1, "reintento recupera factura sin segundo timbrado");
var colision = VentaPrueba("100004"); colision.IdVenta = 999;
var resultadoColision = await servicio.TimbrarVenta(colision, cliente, negocio, carpeta);
Verificar(!resultadoColision.exito && pac.Llamadas == 1, "folio de otra venta bloqueado sin reutilizar UUID");
var timeout = new PacSimulado { Fallar = true };
var servicioTimeout = Servicio(timeout);
await servicioTimeout.TimbrarVenta(VentaPrueba("100005"), cliente, negocio, carpeta);
var reintentoTimeout = await servicioTimeout.TimbrarVenta(VentaPrueba("100005"), cliente, negocio, carpeta);
Verificar(!reintentoTimeout.exito && timeout.Llamadas == 1, "respuesta incierta exige conciliación y bloquea reenvío");
var concurrente = new PacSimulado { Demora = 200 };
var ssl = new PacSimulado { FallarSsl = true };
var servicioSsl = Servicio(ssl);
var resultadoSsl = await servicioSsl.TimbrarVenta(VentaPrueba("100009"), cliente, negocio, carpeta);
Verificar(!resultadoSsl.exito && !File.Exists(Path.Combine(carpeta, "Factura_100009_pendiente")), "fallo SSL previo al envio libera el intento");
ssl.FallarSsl = false;
var reintentoSsl = await servicioSsl.TimbrarVenta(VentaPrueba("100009"), cliente, negocio, carpeta);
Verificar(reintentoSsl.exito && ssl.Llamadas == 2, "permite reintentar tras resolver SSL");
var servicioConcurrente = Servicio(concurrente);
var tareas = await Task.WhenAll(
    servicioConcurrente.TimbrarVenta(VentaPrueba("100006"), cliente, negocio, carpeta),
    servicioConcurrente.TimbrarVenta(VentaPrueba("100006"), cliente, negocio, carpeta));
Verificar(tareas.Any(x => x.exito) && concurrente.Llamadas == 1, "concurrencia permite un solo envío SOAP");
var sinUuid = new PacSimulado { SinUuid = true };
var resultadoSinUuid = await Servicio(sinUuid).TimbrarVenta(VentaPrueba("100007"), cliente, negocio, carpeta);
Verificar(!resultadoSinUuid.exito && !File.Exists(Path.Combine(carpeta, "Factura_100007_resultado.json")), "no confirma ZIP sin UUID");
var totalInvalido = VentaPrueba("100008"); totalInvalido.Total = 999;
var resultadoInvalido = await servicio.TimbrarVenta(totalInvalido, cliente, negocio, carpeta);
Verificar(!resultadoInvalido.exito && pac.Llamadas == 1, "totales inconsistentes bloqueados antes del PAC");
await VerificacionApi.Ejecutar(carpeta, negocio);
Console.WriteLine("Verificación completada sin SQL ni timbrado real. Artefactos: " + carpeta);

internal sealed class PacSimulado : ITimbradoSoapClient
{
    public int Llamadas;
    public bool Fallar, SinUuid, FallarSsl;
    public int Demora;
    public async Task<byte[]> TimbrarF(string usuario, string password, string xmlComprobante)
    {
        Interlocked.Increment(ref Llamadas);
        if (FallarSsl) throw new ConexionSeguraPacException("Fallo SSL simulado", new System.Security.Authentication.AuthenticationException("Certificado expirado"));
        if (Demora > 0) await Task.Delay(Demora);
        if (Fallar) throw new TimeoutException("Respuesta incierta simulada");
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            using (var pdf = zip.CreateEntry("factura.pdf").Open()) pdf.Write(Encoding.ASCII.GetBytes("%PDF-1.4\nSIMULADO"));
            using var escritor = new StreamWriter(zip.CreateEntry("factura.xml").Open());
            escritor.Write(SinUuid ? "<Comprobante/>" : "<Comprobante><TimbreFiscalDigital UUID=\"11111111-1111-1111-1111-111111111111\"/></Comprobante>");
        }
        return buffer.ToArray();
    }
}
