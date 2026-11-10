using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVenta.Entity.Models;

public partial class Venta
{
    public int IdVenta { get; set; }

    public string? NumeroVenta { get; set; }

    public int? IdTipoDocumentoVenta { get; set; }

    public int? IdUsuario { get; set; }

    public int? IdCliente { get; set; }

    public string? DocumentoCliente { get; set; }

    public string? NombreCliente { get; set; }

    public decimal? SubTotal { get; set; }

    public decimal? ImpuestoTotal { get; set; }

    public decimal? Total { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public int? IdMetodoPago { get; set; }

    public int? IdUsoCFDI { get; set; }

    public int? IdFormaPago { get; set; }

    [NotMapped]
    public string? ClaveFormaPago { get; set; }

    [NotMapped]
    public string? FormaPago { get; set; }

    [NotMapped]
    public string? ClaveMetodoPago { get; set; }

    [NotMapped]
    public string? MetodoPago { get; set; }

    [NotMapped]
    public string? ClaveUsoCFDI { get; set; }

    [NotMapped]
    public string? UsoCFDI { get; set; }

    public string? RutaXML { get; set; }

    public string? RutaPDF { get; set; }

    public string? UUID { get; set; }

    public DateTime? FechaTimbrado { get; set; }

    public virtual ICollection<DetalleVenta> DetalleVenta { get; set; } = new List<DetalleVenta>();

    public virtual TipoDocumentoVenta? IdTipoDocumentoVentaNavigation { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual Cliente? IdClienteNavigation { get; set; }
}