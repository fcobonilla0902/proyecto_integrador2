namespace SistemaVenta.AplicacionWeb.Models.ViewModels
{
    public class VMNegocio
    {
        public int IdNegocio { get; set; }

        public string? UrlLogo { get; set; }

        public string? Rfc { get; set; }

        public string? Nombre { get; set; }

        public string? Correo { get; set; }

        public string? Direccion { get; set; }

        public string? Telefono { get; set; }

        public string? CodigoPostal { get; set; }

        public int? IdPais { get; set; }

        public string? Pais { get; set; }

        public int? IdEstado { get; set; }

        public string? EstadoNombre { get; set; }

        public int? IdMunicipio { get; set; }

        public string? Municipio { get; set; }

        public string? SimboloMoneda { get; set; }

        public string? RegimenFiscal { get; set; }
    }
}