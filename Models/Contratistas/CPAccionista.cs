using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Contratistas
{
    [Table("CP_Accionistas")]
    public class CPAccionista
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ContratistProveedorId { get; set; }

        [Required, MaxLength(200)] public string NombreRazonSocial { get; set; } = string.Empty;
        [Required, MaxLength(20)]  public string TipoDocumento { get; set; } = string.Empty;
        [Required, MaxLength(50)]  public string NumeroDocumento { get; set; } = string.Empty;
        public bool TieneCategoriaPEP { get; set; } = false;

        [ForeignKey("ContratistProveedorId")]
        public ContratistaProveedor? ContratistaProveedor { get; set; }
    }
}