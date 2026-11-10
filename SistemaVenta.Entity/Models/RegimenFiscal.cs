using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaVenta.Entity.Models
{
    public partial class RegimenFiscal
    {
        public string IdRegimenFiscal { get; set; } = null!; // CHAR(5) -> string
        public string? Descripcion { get; set; }

        public virtual ICollection<Negocio> Negocios { get; set; } = new List<Negocio>();
    }

}

