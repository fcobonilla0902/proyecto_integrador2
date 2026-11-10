using System;
using System.Collections.Generic;

namespace SistemaVenta.Entity.Models;

public partial class Negocio
{
    public int IdNegocio { get; set; }

    public string? UrlLogo { get; set; }

    public string? NombreLogo { get; set; }

    public string? Rfc { get; set; }

    public string? Nombre { get; set; }

    public string? Correo { get; set; }

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public string? CodigoPostal { get; set; }

    public string? SimboloMoneda { get; set; }

    public string? RegimenFiscal { get; set; }

    public string? UsuarioPac { get; set; }

    public string? PasswordPac { get; set; }

    public string? SerieFactura { get; set; }

    public int? FolioInicialFactura { get; set; }

    public int? IdCodigoPostal { get; set; }
    public int? IdRegimenFiscal { get; set; }
    public int? IdPais { get; set; }
    public int? IdEstado { get; set; }
    public int? IdMunicipio { get; set; }
}
