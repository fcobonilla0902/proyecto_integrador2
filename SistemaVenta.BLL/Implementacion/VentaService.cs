using Microsoft.EntityFrameworkCore;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.DAL.DBContext;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity;
using SistemaVenta.Entity.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaVenta.BLL.Implementacion
{
    public class VentaService : IVentaService
    {
        private readonly IGenericRepository<Producto> _repositorioProducto;
        private readonly IVentaRepository _repositorioVenta;
        private readonly DbventaContext _dbContext; //  v minúscula

        public VentaService(
            IGenericRepository<Producto> repositorioProducto,
            IVentaRepository repositorioVenta,
            DbventaContext dbContext  //  v minúscula
        )
        {
            _repositorioProducto = repositorioProducto;
            _repositorioVenta = repositorioVenta;
            _dbContext = dbContext;
        }

        public async Task<List<Producto>> ObtenerProductos(string busqueda)
        {
            IQueryable<Producto> query = await _repositorioProducto.Consultar(
                p => p.EsActivo == true &&
                p.Stock > 0 &&
                string.Concat(p.CodigoBarra, p.Marca, p.Descripcion).Contains(busqueda)
                );

            return query.Include(c => c.IdCategoriaNavigation).ToList();
        }

        public async Task<Venta> Registrar(Venta entidad)
        {
            try
            {
                return await _repositorioVenta.Registrar(entidad);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Producto?> ObtenerProductoPorId(int idProducto)
        {
            return await _repositorioProducto.Obtener(p => p.IdProducto == idProducto);
        }

        public async Task<List<Venta>> Historial(string numeroVenta, string fechaInicio, string fechaFin)
        {
            IQueryable<Venta> query = await _repositorioVenta.Consultar();
            fechaInicio = fechaInicio is null ? "" : fechaInicio;
            fechaFin = fechaFin is null ? "" : fechaFin;

            if (fechaInicio != "" && fechaFin != "")
            {
                DateTime fech_inicio = DateTime.ParseExact(fechaInicio, "dd/MM/yyyy", new CultureInfo("es-PE"));
                DateTime fech_fin = DateTime.ParseExact(fechaFin, "dd/MM/yyyy", new CultureInfo("es-PE"));

                return query.Where(v =>
                    v.FechaRegistro.Value.Date >= fech_inicio.Date &&
                    v.FechaRegistro.Value.Date <= fech_fin.Date
                )
                    .Include(tdv => tdv.IdTipoDocumentoVentaNavigation)
                    .Include(u => u.IdUsuarioNavigation)
                    .Include(dv => dv.DetalleVenta)
                    .ToList();
            }
            else
            {
                return query.Where(v => v.NumeroVenta == numeroVenta
                )
                    .Include(tdv => tdv.IdTipoDocumentoVentaNavigation)
                    .Include(u => u.IdUsuarioNavigation)
                    .Include(dv => dv.DetalleVenta)
                    .ToList();
            }
        }

        public async Task<Venta> Detalle(string numeroVenta)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("[DEBUG DB CONTEXT] ConnectionString:");
            Console.WriteLine(_dbContext.Database.GetDbConnection().ConnectionString);
            Console.WriteLine("========================================");

            Console.WriteLine("========================================");
            Console.WriteLine("[DEBUG DB CONTEXT] Database:");
            Console.WriteLine(_dbContext.Database.GetDbConnection().Database);
            Console.WriteLine("========================================");
            IQueryable<Venta> query = await _repositorioVenta.Consultar(v => v.NumeroVenta == numeroVenta);

            return query
                    .Include(tdv => tdv.IdTipoDocumentoVentaNavigation)
                    .Include(u => u.IdUsuarioNavigation)
                    .Include(dv => dv.DetalleVenta)
                    .First();
        }

        public async Task<List<DetalleVenta>> Reporte(string fechaInicio, string fechaFin)
        {
            DateTime fech_inicio = DateTime.ParseExact(fechaInicio, "dd/MM/yyyy", new CultureInfo("es-PE"));
            DateTime fech_fin = DateTime.ParseExact(fechaFin, "dd/MM/yyyy", new CultureInfo("es-PE"));

            List<DetalleVenta> lista = await _repositorioVenta.Reporte(fech_inicio, fech_fin);

            return lista;
        }

        public async Task<bool> ActualizarRutasFactura(int idVenta, string rutaPDF, string rutaXML, string uuid)
        {
            try
            {
                Console.WriteLine($"========================================");
                Console.WriteLine($"[ACTUALIZAR RUTAS] Iniciando actualización...");
                Console.WriteLine($"[ACTUALIZAR RUTAS] IdVenta: {idVenta}");
                Console.WriteLine($"[ACTUALIZAR RUTAS] RutaPDF: {rutaPDF}");
                Console.WriteLine($"[ACTUALIZAR RUTAS] RutaXML: {rutaXML}");
                Console.WriteLine($"[ACTUALIZAR RUTAS] UUID: {uuid}");

                var ventaExistente = await _dbContext.Venta.FindAsync(idVenta);

                if (ventaExistente == null)
                {
                    Console.WriteLine($"[ACTUALIZAR RUTAS]  Venta no encontrada en DbContext");
                    Console.WriteLine($"========================================");
                    return false;
                }

                Console.WriteLine($"[ACTUALIZAR RUTAS]  Venta encontrada: {ventaExistente.NumeroVenta}");

                ventaExistente.RutaPDF = rutaPDF;
                ventaExistente.RutaXML = rutaXML;
                ventaExistente.UUID = uuid;
                ventaExistente.FechaTimbrado = DateTime.Now;

                Console.WriteLine($"[ACTUALIZAR RUTAS] Guardando cambios...");

                int filasAfectadas = await _dbContext.SaveChangesAsync();

                Console.WriteLine($"[ACTUALIZAR RUTAS] Filas afectadas: {filasAfectadas}");

                if (filasAfectadas > 0)
                {
                    Console.WriteLine($"[ACTUALIZAR RUTAS]  ACTUALIZACIÓN EXITOSA ");
                    Console.WriteLine($"========================================");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[ACTUALIZAR RUTAS]  No se actualizaron filas");
                    Console.WriteLine($"========================================");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ACTUALIZAR RUTAS]  Exception: {ex.Message}");
                Console.WriteLine($"[ACTUALIZAR RUTAS] Stack: {ex.StackTrace}");
                Console.WriteLine($"========================================");
                return false;
            }
        }
        public async Task<bool> ConvertirTicketAFactura(int idVenta)
        {
            try
            {
                Console.WriteLine($"========================================");
                Console.WriteLine($"[CONVERTIR TICKET] Iniciando conversión...");
                Console.WriteLine($"[CONVERTIR TICKET] IdVenta: {idVenta}");

                //  Buscar la venta en el DbContext directamente
                var ventaExistente = await _dbContext.Venta.FindAsync(idVenta);

                if (ventaExistente == null)
                {
                    Console.WriteLine($"[CONVERTIR TICKET]  Venta no encontrada");
                    Console.WriteLine($"========================================");
                    return false;
                }

                Console.WriteLine($"[CONVERTIR TICKET]  Venta encontrada: {ventaExistente.NumeroVenta}");
                Console.WriteLine($"[CONVERTIR TICKET] Tipo actual: {ventaExistente.IdTipoDocumentoVenta}");

                //  Cambiar de Ticket (1) a Factura (2)
                ventaExistente.IdTipoDocumentoVenta = 2;

                Console.WriteLine($"[CONVERTIR TICKET] Guardando cambios...");

                //  Guardar cambios con DbContext
                int filasAfectadas = await _dbContext.SaveChangesAsync();

                Console.WriteLine($"[CONVERTIR TICKET] Filas afectadas: {filasAfectadas}");

                if (filasAfectadas > 0)
                {
                    Console.WriteLine($"[CONVERTIR TICKET]  CONVERSIÓN EXITOSA ");
                    Console.WriteLine($"[CONVERTIR TICKET] Nuevo tipo: Factura (2)");
                    Console.WriteLine($"========================================");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[CONVERTIR TICKET]  No se actualizaron filas");
                    Console.WriteLine($"========================================");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONVERTIR TICKET]  Exception: {ex.Message}");
                Console.WriteLine($"[CONVERTIR TICKET] Stack: {ex.StackTrace}");
                Console.WriteLine($"========================================");
                return false;
            }
        }


    }
}
