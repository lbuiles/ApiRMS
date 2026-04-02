using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tracker
{
    [Table("Proyectos_Observaciones")]
    public class ProyectoObservacion
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProyectoId { get; set; }

        [Required]
        public Guid UsuarioId { get; set; }

        [Required]
        public string Observacion { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // --- Relaciones ---
        [ForeignKey("ProyectoId")]
        public Proyecto? Proyecto { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }
    }
}