using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Contratistas
{
    [Table("CP_Referencias")]
    public class CPReferencia
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ContratistProveedorId { get; set; }

        [Required, MaxLength(200)] public string Entidad { get; set; } = string.Empty;
        [MaxLength(150)] public string? NombreContacto { get; set; }
        [MaxLength(30)]  public string? Telefono { get; set; }

        [ForeignKey("ContratistProveedorId")]
        public ContratistaProveedor? ContratistaProveedor { get; set; }
    }
}