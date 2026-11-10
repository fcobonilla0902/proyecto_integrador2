using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaVenta.Entity.Models
{
    public class Cliente
    {
        public int IdCliente { get; set; }

        public string? Nombre { get; set; }

        public string? Correo { get; set; }

        public string? Rfc { get; set; }

        public string? DomicilioFiscalReceptor { get; set; }

        public string? RegimenFiscalReceptor { get; set; }

        public bool? EsActivo { get; set; }

        public DateTime FechaRegistro { get; set; }
    }

}
