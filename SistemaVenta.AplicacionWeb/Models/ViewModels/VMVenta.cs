using System;
using System.Collections.Generic;

namespace SistemaVenta.AplicacionWeb.Models.ViewModels
{
    public class VMVenta
    {
        public int IdVenta { get; set; }
        public string? NumeroVenta { get; set; }

        public int? IdTipoDocumentoVenta { get; set; }
        public string? TipoDocumentoVenta { get; set; }

        public int? IdUsuario { get; set; }
        public string? Usuario { get; set; }

        public string? DocumentoCliente { get; set; }
        public string? NombreCliente { get; set; }

        public string? SubTotal { get; set; }
        public string? DescuentoTotal { get; set; }
        public string? ImpuestoTotal { get; set; }
        public string? Total { get; set; }
        public string? FechaRegistro { get; set; }

        public int? IdCliente { get; set; }

        public string? RutaPDF { get; set; }
        public string? RutaXML { get; set; }
        public string? UUID { get; set; }
        public string? FechaTimbrado { get; set; }

        public int? IdTipoDeComprobante { get; set; }
        public string? TipoDeComprobante { get; set; }
        public string? ClaveTipoDeComprobante { get; set; }

        public int? IdMetodoPago { get; set; }
        public string? MetodoPago { get; set; }
        public string? ClaveMetodoPago { get; set; }

        public int? IdUsoCFDI { get; set; }
        public string? UsoCFDI { get; set; }
        public string? ClaveUsoCFDI { get; set; }

        public int? IdFormaPago { get; set; }
        public string? FormaPago { get; set; }
        public string? ClaveFormaPago { get; set; }

        public string? RfcCliente { get; set; }
        public string? DireccionFiscal { get; set; }
        public string? CorreoElectronicoCliente { get; set; }

        public int? IdCodigoPostal { get; set; }
        public string? CodigoPostal { get; set; }

        public int? IdPais { get; set; }
        public string? Pais { get; set; }
        public string? ClavePais { get; set; }

        public int? IdEstado { get; set; }
        public string? Estado { get; set; }
        public string? ClaveEstado { get; set; }

        public int? IdMunicipio { get; set; }
        public string? Municipio { get; set; }
        public string? ClaveMunicipio { get; set; }

        public int? IdRegimenFiscal { get; set; }
        public string? RegimenFiscal { get; set; }
        public string? ClaveRegimenFiscal { get; set; }

        public List<VMDetalleVenta>? DetalleVenta { get; set; }
    }
}