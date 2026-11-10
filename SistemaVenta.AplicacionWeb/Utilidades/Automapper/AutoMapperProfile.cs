
using SistemaVenta.AplicacionWeb.Models.ViewModels;
using System.Globalization;
using SistemaVenta.Entity.Models;

using AutoMapper;

namespace SistemaVenta.AplicacionWeb.Utilidades.Automapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            #region Rol
            CreateMap<Rol, VMRol>().ReverseMap();
            #endregion Rol

            #region Usuario
            CreateMap<Usuario, VMUsuario>()
              .ForMember(destino =>
                destino.EsActivo,
                opt => opt.MapFrom(origen => origen.EsActivo == true ? 1 : 0)
                )
              .ForMember(destino =>
                 destino.NombreRol,
                 opt => opt.MapFrom(origen => origen.IdRolNavigation.Descripcion)

                 );

            CreateMap<VMUsuario, Usuario>()
                .ForMember(destino =>
                destino.EsActivo,
                opt => opt.MapFrom(origen => origen.EsActivo == 1 ? true : false)
                )
                .ForMember(destino =>
                  destino.IdRolNavigation,
                  opt => opt.Ignore()
                  );

            #endregion

            #region Negocio
            CreateMap<Negocio, VMNegocio>().ReverseMap();
            #endregion

            #region Categoria
            CreateMap<Categoria, VMCategoria>()
               .ForMember(destino =>
               destino.esActivo,
               opt => opt.MapFrom(origen => origen.EsActivo == true ? 1 : 0)
            );

            CreateMap<VMCategoria, Categoria>()
               .ForMember(destino =>
               destino.EsActivo,
               opt => opt.MapFrom(origen => origen.esActivo == 1 ? true : false)
            );
            #endregion

            #region Producto

            CreateMap<Producto, VMProducto>()
                .ForMember(destino =>
                  destino.EsActivo,
                  opt => opt.MapFrom(origen => origen.EsActivo == true ? 1 : 0)
                )
                .ForMember(destino =>
                  destino.NombreCategoria,
                  opt => opt.MapFrom(origen => origen.IdCategoriaNavigation.Descripcion)
                )
                .ForMember(destino =>
                  destino.Precio,
                  opt => opt.MapFrom(origen => Convert.ToString(origen.Precio.Value, new CultureInfo("es-PE")))
                );

            CreateMap<VMProducto, Producto>()
                .ForMember(destino =>
                  destino.EsActivo,
                  opt => opt.MapFrom(origen => origen.EsActivo == 1 ? true : false)
                )
                .ForMember(destino =>
                  destino.IdCategoriaNavigation,
                  opt => opt.Ignore()
                )
                .ForMember(destino =>
                  destino.Precio,
                  opt => opt.MapFrom(origen => Convert.ToDecimal(origen.Precio, new CultureInfo("es-PE")))
                );

            #endregion

            #region TipoDocumentoVenta
            CreateMap<TipoDocumentoVenta, VMTipoDocumentoVenta>().ReverseMap();
            #endregion

            #region Venta
            CreateMap<Venta, VMVenta>()
                .ForMember(destino =>
                    destino.IdTipoDocumentoVenta,
                    opt => opt.MapFrom(origen => origen.IdTipoDocumentoVenta)
                )
                .ForMember(destino =>
                    destino.TipoDocumentoVenta,
                    opt => opt.MapFrom(origen => origen.IdTipoDocumentoVentaNavigation.Descripcion)
                )
                .ForMember(destino =>
                    destino.Usuario,
                    opt => opt.MapFrom(origen => origen.IdUsuarioNavigation.Nombre)
                )
                .ForMember(destino =>
                    destino.SubTotal,
                    opt => opt.MapFrom(origen => Convert.ToString(origen.SubTotal.Value, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.ImpuestoTotal,
                    opt => opt.MapFrom(origen => Convert.ToString(origen.ImpuestoTotal.Value, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.Total,
                    opt => opt.MapFrom(origen => Convert.ToString(origen.Total.Value, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.FechaRegistro,
                    opt => opt.MapFrom(origen => origen.FechaRegistro.Value.ToString("dd/MM/yyyy"))
                )
                //  MAPEOS NUEVOS
                .ForMember(destino =>
                    destino.IdCliente,
                    opt => opt.MapFrom(origen => origen.IdCliente)
                )
                .ForMember(destino =>
                    destino.RutaPDF,
                    opt => opt.MapFrom(origen => origen.RutaPDF)
                )
                .ForMember(destino =>
                    destino.RutaXML,
                    opt => opt.MapFrom(origen => origen.RutaXML)
                )
                .ForMember(destino =>
                    destino.UUID,
                    opt => opt.MapFrom(origen => origen.UUID)
                );

            CreateMap<VMVenta, Venta>()
                .ForMember(destino =>
                    destino.SubTotal,
                    opt => opt.MapFrom(origen => Convert.ToDecimal(origen.SubTotal, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.ImpuestoTotal,
                    opt => opt.MapFrom(origen => Convert.ToDecimal(origen.ImpuestoTotal, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.Total,
                    opt => opt.MapFrom(origen => Convert.ToDecimal(origen.Total, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.IdCliente,
                    opt => opt.MapFrom(origen => origen.IdCliente)
                )
                .ForMember(destino =>
                    destino.IdMetodoPago,
                    opt => opt.MapFrom(origen => origen.IdMetodoPago)
                )
                .ForMember(destino =>
                    destino.IdUsoCFDI,
                    opt => opt.MapFrom(origen => origen.IdUsoCFDI)
                )
                .ForMember(destino =>
                    destino.IdFormaPago,
                    opt => opt.MapFrom(origen => origen.IdFormaPago)
                );
            #endregion

            #region DetalleVenta
            CreateMap<DetalleVenta, VMDetalleVenta>()
                .ForMember(destino =>
                    destino.Precio,
                    opt => opt.MapFrom(origen => Convert.ToString(origen.Precio.Value, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.Total,
                    opt => opt.MapFrom(origen => Convert.ToString(origen.Total.Value, new CultureInfo("es-PE")))
                );

            CreateMap<VMDetalleVenta, DetalleVenta>()
                .ForMember(destino =>
                    destino.Precio,
                    opt => opt.MapFrom(origen => Convert.ToDecimal(origen.Precio, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.Total,
                    opt => opt.MapFrom(origen => Convert.ToDecimal(origen.Total, new CultureInfo("es-PE")))
                );

            CreateMap<DetalleVenta, VMReporteVenta>()
                .ForMember(destino =>
                    destino.FechaRegistro,
                    opt => opt.MapFrom(origen => origen.IdVentaNavigation.FechaRegistro.Value.ToString("dd/MM/yyyy"))
                )
                .ForMember(destino =>
                    destino.NumeroVenta,
                    opt => opt.MapFrom(origen => origen.IdVentaNavigation.NumeroVenta)
                )
                .ForMember(destino =>
                    destino.TipoDocumento,
                    opt => opt.MapFrom(origen => origen.IdVentaNavigation.IdTipoDocumentoVentaNavigation.Descripcion)
                )
                .ForMember(destino =>
                    destino.DocumentoCliente,
                    opt => opt.MapFrom(origen => origen.IdVentaNavigation.DocumentoCliente)
                )
                .ForMember(destino =>
                    destino.NombreCliente,
                    opt => opt.MapFrom(origen => origen.IdVentaNavigation.NombreCliente)
                )
                .ForMember(destino =>
                    destino.SubTotalVenta,
                    opt => opt.MapFrom(origen => Convert.ToString(origen.IdVentaNavigation.SubTotal.Value, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.ImpuestoTotalVenta,
                    opt => opt.MapFrom(origen => Convert.ToString(origen.IdVentaNavigation.ImpuestoTotal.Value, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.TotalVenta,
                    opt => opt.MapFrom(origen => Convert.ToString(origen.IdVentaNavigation.Total.Value, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.Producto,
                    opt => opt.MapFrom(origen => origen.DescripcionProducto)
                )
                .ForMember(destino =>
                    destino.Precio,
                    opt => opt.MapFrom(origen => Convert.ToString(origen.Precio.Value, new CultureInfo("es-PE")))
                )
                .ForMember(destino =>
                    destino.Total,
                    opt => opt.MapFrom(origen => Convert.ToString(origen.Total.Value, new CultureInfo("es-PE")))
                );

            #endregion

            #region Menu
            CreateMap<Menu, VMMenu>()
                .ForMember(destino =>
                  destino.SubMenus,
                  opt => opt.MapFrom(origen => origen.InverseIdMenuPadreNavigation)
                );

            #endregion

            #region Cliente
            CreateMap<Cliente, VMCliente>()
                .ForMember(destino => destino.EsActivo, opt => opt.MapFrom((origen => origen.EsActivo == true ? 1 : 0)));
            CreateMap<VMCliente, Cliente>()
                .ForMember(destino => destino.EsActivo, opt => opt.MapFrom((origen => origen.EsActivo == 1 ? true : false)));

            #endregion

            CreateMap<Cliente, ClienteDTO>()
            .ForMember(dest => dest.FechaRegistro,
               opt => opt.MapFrom(src => src.FechaRegistro.ToString("yyyy-MM-dd")));
        }


    }
}
