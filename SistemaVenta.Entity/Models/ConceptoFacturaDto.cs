namespace SistemaVenta.Entity.Models
{
    public class ConceptoFacturaDto
    {
        public int IdProducto { get; set; }
        public string? Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal ImpuestoTasa { get; set; }

        public string? ClaveProdServ { get; set; }
        public string? ClaveUnidad { get; set; }
        public string? Unidad { get; set; }
        public string? ObjetoImp { get; set; }
        public string? TipoFactor { get; set; }
        public string? Impuesto { get; set; }
        public string? TipoImpuesto { get; set; }
    }
}