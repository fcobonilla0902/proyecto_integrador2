using Microsoft.EntityFrameworkCore;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity;
using SistemaVenta.Entity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SistemaVenta.BLL.Implementacion
{
    public class ClienteService : IClienteService
    {
        private readonly IGenericRepository<Cliente> _repositorio;

        public ClienteService(
            IGenericRepository<Cliente> repositorio
            )
        {
            _repositorio = repositorio;
        }
        public async Task<Cliente> Crear(Cliente entidad)
        {

            Cliente cliente_existe = await _repositorio.Obtener(c => c.Rfc == entidad.Rfc);

            if (cliente_existe != null)
                throw new TaskCanceledException("El ciente (RFC) ya existe");


            try
            {

                Cliente cliente_creado = await _repositorio.Crear(entidad);

                if (cliente_creado.IdCliente == 0)
                    throw new TaskCanceledException("No se pudo crear el Cliente");

                IQueryable<Cliente> query = await _repositorio.Consultar(c => c.IdCliente == cliente_creado.IdCliente);
                cliente_creado = query.First();

                return cliente_creado;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public async Task<Cliente> Editar(Cliente entidad)
        {
            Cliente cliente_existe = await _repositorio.Obtener(u => u.Correo == entidad.Correo && u.IdCliente != entidad.IdCliente);

            if (cliente_existe != null)
                throw new TaskCanceledException("El correo ya existe");


            try
            {

                IQueryable<Cliente> queryCliente = await _repositorio.Consultar(c => c.IdCliente == entidad.IdCliente);

                Cliente cliente_editar = queryCliente.First();

                cliente_editar.Nombre = entidad.Nombre;
                cliente_editar.Correo = entidad.Correo;
                cliente_editar.Rfc = entidad.Rfc;
                cliente_editar.DomicilioFiscalReceptor = entidad.DomicilioFiscalReceptor;
                cliente_editar.EsActivo = entidad.EsActivo;
                cliente_editar.RegimenFiscalReceptor = entidad.RegimenFiscalReceptor;


                bool respuesta = await _repositorio.Editar(cliente_editar);

                if (!respuesta)
                    throw new TaskCanceledException("No se pudo modificar el Cliente:(");

                Cliente cliente_editado = queryCliente.First();

                return cliente_editado;

            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> Eliminar(int idCliente)
        {
            try
            {
                Cliente cliente_encontrado = await _repositorio.Obtener(u => u.IdCliente == idCliente);

                if (cliente_encontrado == null)
                    throw new TaskCanceledException("El cliente no existe");


                bool respuesta = await _repositorio.Eliminar(cliente_encontrado);

                return true;

            }
            catch
            {
                throw;
            }
        }

        public async Task<List<Cliente>> Lista()
        {
            IQueryable<Cliente> query = await _repositorio.Consultar();
            return query.ToList();
        }

        public async Task<Cliente> ObtenerPorId(int idCliente)
        {
            IQueryable<Cliente> query = await _repositorio.Consultar(u => u.IdCliente == idCliente);

            Cliente cliente = query.FirstOrDefault();

            return cliente;


        }

        public async Task<Cliente> Obtener(Expression<Func<Cliente, bool>> filtro)
        {
            return await _repositorio.Obtener(filtro);
        }
    }
}
