using SistemaVenta.Entity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SistemaVenta.BLL.Interfaces
{
    public interface IClienteService
    {
        Task<List<Cliente>> Lista();

        Task<Cliente> Crear(Cliente entidad);

        Task<Cliente> Editar(Cliente entidad);

        Task<bool> Eliminar(int idCliente);

        Task<Cliente> ObtenerPorId(int idCliente);

        Task<Cliente> Obtener(Expression<Func<Cliente, bool>> filtro);

    }
}
