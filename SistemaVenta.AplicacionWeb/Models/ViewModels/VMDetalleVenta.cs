namespace SistemaVenta.AplicacionWeb.Models.ViewModels
{
    public class VMDetalleVenta
    {
        public int IdProducto { get; set; }
        public string? DescripcionProducto { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal DescuentoDinero { get; set; }
        public decimal Impuesto { get; set; }
        public decimal ImpuestoDinero { get; set; }
        public decimal Total { get; set; }

        // Nuevos campos SAT
        public string? ClaveProdServSat { get; set; }
        public string? ClaveUnidadSat { get; set; }
        public string? ObjetoImpuesto { get; set; }
        public string? TipoImpuesto { get; set; }
        public string? FactorImpuesto { get; set; }
        public decimal? ValorImpuesto { get; set; }
        public string? Unidad { get; set; }
    }
}