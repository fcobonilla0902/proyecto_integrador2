using SistemaVenta.Entity;

namespace SistemaVenta.AplicacionWeb.Models.ViewModels
{
    public class VMProducto
    {
        public int IdProducto { get; set; }
        public string? CodigoBarra { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Descripcion { get; set; }
        public int? IdCategoria { get; set; }
        public string? NombreCategoria { get; set; }
        public int? Stock { get; set; }
        public string? UrlImagen { get; set; }
        public string? NombreImagen { get; set; }
        public string? Precio { get; set; }
        public int? EsActivo { get; set; }
        public string? MedidaEmpresa { get; set; }
        public string? MedidaSat { get; set; }
        public string? ClaveProductoSat { get; set; }
        public string? ObjetoImpuesto { get; set; }
        public string? FactorImpuesto { get; set; }
        public string? Impuesto { get; set; }
        public decimal? ValorImpuesto { get; set; }
        public decimal? Descuento { get; set; }

        public int? IdMarca { get; set; }
        public int? IdModelo { get; set; }
        public int? IdClaveUnidadSAT { get; set; }
        public int? IdClaveProdServSAT { get; set; }
        public int? IdObjetoImpuestoSAT { get; set; }
        public int? IdImpuestoSAT { get; set; }
        public int? IdTipoFactorSAT { get; set; }
        public int? IdMedidaLocal { get; set; }
        public int? IdMoneda { get; set; }
        public string? Moneda { get; set; }

        public DateTime? FechaRegistro { get; set; }
        public string? FechaRegistroTexto { get; set; }
    }
}