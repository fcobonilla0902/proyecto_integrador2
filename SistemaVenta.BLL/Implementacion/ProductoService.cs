using Microsoft.EntityFrameworkCore;
using SistemaVenta.BLL.Interfaces;
using SistemaVenta.DAL.Interfaces;
using SistemaVenta.Entity.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaVenta.BLL.Implementacion
{
    public class ProductoService : IProductoService
    {
        private readonly IGenericRepository<Producto> _repositorio;
        private readonly ILocalStorageService _cloudinaryServicio;

        public ProductoService(IGenericRepository<Producto> repositorio, ILocalStorageService cloudinaryServicio)
        {
            _repositorio = repositorio;
            _cloudinaryServicio = cloudinaryServicio;
        }

        // ====== LISTA ======
        public async Task<List<Producto>> Lista()
        {
            IQueryable<Producto> query = await _repositorio.Consultar();
            return query.Include(c => c.IdCategoriaNavigation).ToList();
        }

        // ====== CREAR ======
        public async Task<Producto> Crear(Producto entidad, Stream imagen = null, string NombreImagen = "")
        {
            Producto producto_existe = await _repositorio.Obtener(p => p.CodigoBarra == entidad.CodigoBarra);

            if (producto_existe != null)
                throw new TaskCanceledException("El código de barra ya existe");

            try
            {
                // === Campos de imagen ===
                entidad.NombreImagen = NombreImagen;
                if (imagen != null)
                {
                    string urlImagen = await _cloudinaryServicio.SubirStorage(imagen, "carpeta_producto", NombreImagen);
                    entidad.UrlImagen = urlImagen;
                }

                // === Guardar producto ===
                Producto producto_creado = await _repositorio.Crear(entidad);

                if (producto_creado.IdProducto == 0)
                    throw new TaskCanceledException("No se pudo crear el producto");

                // === Recargar con navegación ===
                IQueryable<Producto> query = await _repositorio.Consultar(p => p.IdProducto == producto_creado.IdProducto);
                producto_creado = query.Include(c => c.IdCategoriaNavigation).First();

                return producto_creado;
            }
            catch
            {
                throw;
            }
        }

        // ====== EDITAR ======
        public async Task<Producto> Editar(Producto entidad, Stream imagen = null, string NombreImagen = "")
        {
            Producto producto_existe = await _repositorio.Obtener(p => p.CodigoBarra == entidad.CodigoBarra && p.IdProducto != entidad.IdProducto);

            if (producto_existe != null)
                throw new TaskCanceledException("El código de barra ya existe");

            try
            {
                IQueryable<Producto> queryProducto = await _repositorio.Consultar(p => p.IdProducto == entidad.IdProducto);
                Producto producto_para_editar = queryProducto.First();

                // === Actualización de campos ===
                producto_para_editar.CodigoBarra = entidad.CodigoBarra;
                producto_para_editar.Marca = entidad.Marca;
                producto_para_editar.Descripcion = entidad.Descripcion;
                producto_para_editar.IdCategoria = entidad.IdCategoria;
                producto_para_editar.Stock = entidad.Stock;
                producto_para_editar.Precio = entidad.Precio;
                producto_para_editar.EsActivo = entidad.EsActivo;

                // === Campos SAT nuevos ===
                producto_para_editar.MedidaEmpresa = entidad.MedidaEmpresa;
                producto_para_editar.MedidaSat = entidad.MedidaSat;
                producto_para_editar.ClaveProductoSat = entidad.ClaveProductoSat;
                producto_para_editar.ObjetoImpuesto = entidad.ObjetoImpuesto;
                producto_para_editar.FactorImpuesto = entidad.FactorImpuesto;
                producto_para_editar.Impuesto = entidad.Impuesto;
                producto_para_editar.ValorImpuesto = entidad.ValorImpuesto;
                producto_para_editar.TipoImpuesto = entidad.TipoImpuesto;
                producto_para_editar.Descuento = entidad.Descuento;


                // === Imagen ===
                if (string.IsNullOrEmpty(producto_para_editar.NombreImagen))
                    producto_para_editar.NombreImagen = NombreImagen;

                if (imagen != null)
                {
                    string urlImagen = await _cloudinaryServicio.SubirStorage(imagen, "carpeta_producto", producto_para_editar.NombreImagen);
                    producto_para_editar.UrlImagen = urlImagen;
                }

                bool respuesta = await _repositorio.Editar(producto_para_editar);
                if (!respuesta)
                    throw new TaskCanceledException("No se pudo editar el producto");

                Producto producto_editado = queryProducto.Include(c => c.IdCategoriaNavigation).First();
                return producto_editado;
            }
            catch
            {
                throw;
            }
        }

        // ====== ELIMINAR ======
        public async Task<bool> Eliminar(int idProducto)
        {
            try
            {
                Producto producto_encontrado = await _repositorio.Obtener(p => p.IdProducto == idProducto);

                if (producto_encontrado == null)
                    throw new TaskCanceledException("El producto no existe");

                string urlImagen = producto_encontrado.UrlImagen;
                string nombreImagen = producto_encontrado.NombreImagen;

                bool respuesta = await _repositorio.Eliminar(producto_encontrado);

                if (respuesta && !string.IsNullOrEmpty(urlImagen))
                    await _cloudinaryServicio.EliminarStorage("carpeta_producto", nombreImagen);

                return true;
            }
            catch
            {
                throw;
            }
        }
    }
}
