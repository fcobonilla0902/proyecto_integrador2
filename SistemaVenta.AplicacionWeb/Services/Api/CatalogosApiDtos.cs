namespace SistemaVenta.AplicacionWeb.Services.Api
{
    public class CodigoPostalSATApiDto
    {
        public int IdCodigoPostal { get; set; }
        public string? C_CodigoPostal { get; set; }
        public int? IdEstado { get; set; }
        public int? IdMunicipio { get; set; }
        public int? IdLocalidad { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    public class RegimenFiscalSATApiDto
    {
        public int IdRegimenFiscal { get; set; }
        public string? C_RegimenFiscal { get; set; }
        public string? Descripcion { get; set; }
        public string? Fisica { get; set; }
        public string? Moral { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    public class PaisSATApiDto
    {
        public int IdPais { get; set; }
        public string? C_Pais { get; set; }
        public string? Descripcion { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    public class EstadoSATApiDto
    {
        public int IdEstado { get; set; }
        public string? C_Estado { get; set; }
        public string? NombreDelEstado { get; set; }
        public int? IdPais { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    public class MunicipioSATApiDto
    {
        public int IdMunicipio { get; set; }
        public string? C_Municipio { get; set; }
        public int? IdEstado { get; set; }
        public string? Descripcion { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    // DTOs del catálogo SAT (ejemplo; ajusta a tus nombres reales)
    public class ClaveProdServSATDto
    {
        public int IdClaveProdServ { get; set; }
        public string? C_ClaveProdServ { get; set; }
        public string? Descripcion { get; set; }
        public string? Incluir_IVA_trasladado { get; set; }
        public string? Incluir_IEPS_trasladado { get; set; }
        public string? Complemento_que_debe_incluir { get; set; }
        public DateTime? FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
        public string? Estimulo_Franja_Fronteriza { get; set; }
        public string? Palabras_similares { get; set; }
        public int? IdStatusRegistro { get; set; }
    }

    public class ClaveUnidadSATDto
    {
        public int IdClaveUnidad { get; set; }
        public string? C_ClaveUnidad { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Nota { get; set; }
        public DateTime? FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
        public string? Simbolo { get; set; }
        public int? IdStatusRegistro { get; set; }
    }
}