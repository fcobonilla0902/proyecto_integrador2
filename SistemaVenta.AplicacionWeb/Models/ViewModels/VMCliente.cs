namespace SistemaVenta.AplicacionWeb.Models.ViewModels
{
    public class VMCliente
    {
        public int IdCliente { get; set; }

        public string? Nombre { get; set; }

        public string? Correo { get; set; }

        public string? Rfc { get; set; }

        public string? DireccionFiscal { get; set; }

        public string? CodigoPostal { get; set; }

        public string? DomicilioFiscalReceptor { get; set; }

        public string? RegimenFiscalReceptor { get; set; }

        public int? IdPais { get; set; }

        public string? Pais { get; set; }

        public int? IdEstado { get; set; }

        public string? EstadoNombre { get; set; }

        public int? IdMunicipio { get; set; }

        public string? Municipio { get; set; }

        public int? EsActivo { get; set; }

        public DateTime FechaRegistro { get; set; }
    }

    public class ClienteDTO
    {
        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        public string? Rfc { get; set; }
        public string? DireccionFiscal { get; set; }
        public string? CodigoPostal { get; set; }
        public string? DomicilioFiscalReceptor { get; set; }
        public string? RegimenFiscalReceptor { get; set; }
        public string? Pais { get; set; }
        public string? EstadoNombre { get; set; }
        public string? Municipio { get; set; }
        public string FechaRegistro { get; set; } = string.Empty;
    }
}