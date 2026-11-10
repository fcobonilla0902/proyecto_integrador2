using SistemaVenta.Entity.Models;
using System.Threading.Tasks;

namespace SistemaVenta.BLL.Interfaces
{
    public interface IFacturacionService
    {
        Task<string> ValidarVenta(Venta venta, Cliente cliente, Negocio negocio);
        Task<(bool exito, string rutaPDF, string rutaXML, string uuid, string mensaje)> TimbrarVenta(
            Venta venta,
            Cliente cliente,
            Negocio negocio,
            string rutaGuardado);

        bool ValidarDatosFiscalesCliente(Cliente cliente);

        Task<bool> ValidarConfiguracionNegocio();
    }
}
