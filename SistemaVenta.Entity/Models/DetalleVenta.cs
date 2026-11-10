using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVenta.Entity.Models;

public partial class DetalleVenta
{
    public int IdDetalleVenta { get; set; }

    public int? IdVenta { get; set; }

    public int? IdProducto { get; set; }

    public decimal? Cantidad { get; set; }

    public decimal? Precio { get; set; }

    public decimal? Total { get; set; }

    public string? DescripcionProducto { get; set; }

    public string? MarcaProducto { get; set; }

    public string? CategoriaProducto { get; set; }

    [NotMapped]
    public decimal? SubTotal { get; set; }

    [NotMapped]
    public decimal? Descuento { get; set; }

    [NotMapped]
    public decimal? DescuentoDinero { get; set; }

    [NotMapped]
    public decimal? Impuesto { get; set; }

    [NotMapped]
    public decimal? ImpuestoDinero { get; set; }

    [NotMapped]
    public string? ClaveProdServSat { get; set; }

    [NotMapped]
    public string? ClaveUnidadSat { get; set; }

    [NotMapped]
    public string? ObjetoImpuesto { get; set; }

    [NotMapped]
    public string? TipoImpuesto { get; set; }

    [NotMapped]
    public string? FactorImpuesto { get; set; }

    [NotMapped]
    public decimal? ValorImpuesto { get; set; }

    [NotMapped]
    public string? Unidad { get; set; }

    public virtual Venta? IdVentaNavigation { get; set; }
}