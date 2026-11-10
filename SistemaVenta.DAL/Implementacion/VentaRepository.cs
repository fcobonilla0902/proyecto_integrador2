using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using SistemaVenta.DAL.DBContext;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity;
using SistemaVenta.Entity.Models;


namespace SistemaVenta.DAL.Implementacion
{
    public class VentaRepository : GenericRepository<Venta>, IVentaRepository
    {
        private readonly DbventaContext _dbContext;

        public VentaRepository(DbventaContext dbContex) : base(dbContex)
        {
            _dbContext = dbContex;
        }
        public async Task<IQueryable<Venta>> Consultar(Expression<Func<Venta, bool>> filtro = null)
        {
            IQueryable<Venta> query = _dbContext.Venta
                .Include(v => v.IdUsuarioNavigation)
                .Include(v => v.IdTipoDocumentoVentaNavigation)
                .Include(v => v.DetalleVenta);

            if (filtro != null)
            {
                query = query.Where(filtro);
            }

            return await Task.FromResult(query);
        }


        public Task<Venta> Crear(Venta entidad)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Editar(Venta entidad)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Eliminar(Venta entidad)
        {
            throw new NotImplementedException();
        }

        public Task<Venta> Obtener(Expression<Func<Venta, bool>> filtro)
        {
            throw new NotImplementedException();
        }

        public async Task<Venta> Registrar(Venta entidad)
        {
            Venta ventaGENERADA = new Venta();

            using (var transaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    foreach (DetalleVenta dv in entidad.DetalleVenta)
                    {
                        Producto producto_encontrado = _dbContext.Productos
                            .Where(p => p.IdProducto == dv.IdProducto)
                            .First();

                        int stockActual = producto_encontrado.Stock ?? 0;
                        int cantidadVendida = Convert.ToInt32(dv.Cantidad ?? 0);

                        if (cantidadVendida <= 0)
                            throw new Exception($"La cantidad del producto {dv.IdProducto} no es válida.");

                        if (stockActual < cantidadVendida)
                            throw new Exception($"Stock insuficiente para el producto {dv.IdProducto}.");

                        producto_encontrado.Stock = stockActual - cantidadVendida;
                        _dbContext.Productos.Update(producto_encontrado);
                    }

                    await _dbContext.SaveChangesAsync();

                    NumeroCorrelativo correlativo = _dbContext.NumeroCorrelativos
                        .Where(n => n.Gestion == "venta")
                        .First();

                    correlativo.UltimoNumero = correlativo.UltimoNumero + 1;
                    correlativo.FechaActualizacion = DateTime.Now;

                    _dbContext.NumeroCorrelativos.Update(correlativo);
                    await _dbContext.SaveChangesAsync();

                    string ceros = string.Concat(Enumerable.Repeat("0", correlativo.CantidadDigitos.Value));
                    string numeroVenta = ceros + correlativo.UltimoNumero.ToString();
                    numeroVenta = numeroVenta.Substring(
                        numeroVenta.Length - correlativo.CantidadDigitos.Value,
                        correlativo.CantidadDigitos.Value
                    );

                    entidad.NumeroVenta = numeroVenta;

                    await _dbContext.Venta.AddAsync(entidad);
                    await _dbContext.SaveChangesAsync();

                    ventaGENERADA = entidad;

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return ventaGENERADA;
        }

        public async Task<List<DetalleVenta>> Reporte(DateTime FechaInicio, DateTime FechaFin)
        {
            List<DetalleVenta> listaResumen = await _dbContext.DetalleVenta
                .Include(v => v.IdVentaNavigation)
                .ThenInclude(u => u.IdUsuarioNavigation)
                .Include(v => v.IdVentaNavigation)
                .ThenInclude(tdv => tdv.IdTipoDocumentoVentaNavigation)
                .Where(dv => dv.IdVentaNavigation.FechaRegistro.Value.Date >= FechaInicio.Date &&
                dv.IdVentaNavigation.FechaRegistro.Value.Date <= FechaFin.Date).ToListAsync();

            return listaResumen;

                
        }
    }
}
