using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Contratistas
{
    [Table("CP_Documentos")]
    public class CPDocumento
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ContratistProveedorId { get; set; }

        [Required, MaxLength(100)] public string TipoDocumento { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string Url { get; set; } = string.Empty;
        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

        [ForeignKey("ContratistProveedorId")]
        public ContratistaProveedor? ContratistaProveedor { get; set; }
    }
}