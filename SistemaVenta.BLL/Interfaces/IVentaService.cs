using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SistemaVenta.Entity;
using SistemaVenta.Entity.Models;

namespace SistemaVenta.BLL.Interfaces
{
    public interface IVentaService
    {
        Task<List<Producto>> ObtenerProductos(string busqueda);

        Task<Venta> Registrar(Venta entidad);

        Task<List<Venta>> Historial(string numeroVenta, string fechaInicio, string fechaFin);

        Task<Venta> Detalle(string numeroVenta);

        Task<List<DetalleVenta>> Reporte(string fechaInicio, string fechaFin);

        // NUEVO MÉTODO
        Task<bool> ActualizarRutasFactura(int idVenta, string rutaPDF, string rutaXML, string uuid);
        Task<bool> ConvertirTicketAFactura(int idVenta);

        Task<Producto?> ObtenerProductoPorId(int idProducto);
    }
}
