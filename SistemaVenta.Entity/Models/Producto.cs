using System;
using System.Collections.Generic;

namespace SistemaVenta.Entity.Models
{
    public partial class Producto
    {
        public int IdProducto { get; set; }
        public string? CodigoBarra { get; set; }
        public string? Marca { get; set; }
        public string? Descripcion { get; set; }
        public int? IdCategoria { get; set; }
        public int? Stock { get; set; }
        public string? UrlImagen { get; set; }
        public string? NombreImagen { get; set; }
        public decimal? Precio { get; set; }
        public bool? EsActivo { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public virtual Categoria? IdCategoriaNavigation { get; set; }

        //  CAMPOS FISCALES - TODOS COMO STRING (excepto valorImpuesto y descuento)
        public string? MedidaEmpresa { get; set; }
        public string? MedidaSat { get; set; }
        public string? ClaveProductoSat { get; set; }
        public string? ObjetoImpuesto { get; set; }
        public string? FactorImpuesto { get; set; }
        public string? Impuesto { get; set; }
        public decimal? ValorImpuesto { get; set; }

        //  CAMBIAR A STRING SI EN BD ES VARCHAR/NVARCHAR
        public string? TipoImpuesto { get; set; }  //  STRING (no int)

        public decimal? Descuento { get; set; }



        /*public int idProducto { get; set; }
        public string? skuCodigodeBarras { get; set; }
        public int? idMarca { get; set; }
        public int? idModelo { get; set; }
        public string? descripcion { get; set; }
        public int? idCategoria { get; set; }
        public int? stock { get; set; }
        public string? urlImagen { get; set; }
        public string? nombreImagen { get; set; }
        public decimal? precioUnitarioCompra { get; set; }
        public decimal? precioUnitarioVenta {  get; set; }
        public int? idMoneda { get; set; }
        public int? idStatusRegistro { get; set; }
        public DateTime? fechaRegistro { get; set; }
        public virtual Categoria? IdCategoriaNavigation { get; set; }

        //  CAMPOS FISCALES - TODOS COMO STRING (excepto valorImpuesto y descuento)
        public int? idMedidaLocal { get; set; }
        public int? idClaveUnidadSAT { get; set; }
        public int? idClaveProdServSAT { get; set; }
        public int? idObjetoImpuestoSAT { get; set; }
        public int? idImpuestoSAT { get; set; }
        public int? idTipoFactorSAT { get; set; }
        public decimal? impuestoProducto { get; set; }*/
    }
}
