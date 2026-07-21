using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Contratistas
{
    [Table("CP_CuentasBancarias")]
    public class CPCuentaBancaria
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ContratistProveedorId { get; set; }

        [Required, MaxLength(30)]  public string TipoProducto { get; set; } = string.Empty;
        [Required, MaxLength(30)]  public string NumeroCuenta { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string Entidad { get; set; } = string.Empty;
        [MaxLength(80)]  public string? Ciudad { get; set; }
        [MaxLength(80)]  public string? Departamento { get; set; }
        [MaxLength(80)]  public string? Pais { get; set; } = "Colombia";
        [MaxLength(300)] public string? Observaciones { get; set; }

        [ForeignKey("ContratistProveedorId")]
        public ContratistaProveedor? ContratistaProveedor { get; set; }
    }
}