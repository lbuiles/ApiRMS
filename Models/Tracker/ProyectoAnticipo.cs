using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tracker
{
    [Table("Proyectos_Anticipos")]
    public class ProyectoAnticipo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProyectoId { get; set; }

        [Required]
        public Guid OrdenTrabajoId { get; set; }

        [Required]
        public Guid SolicitanteId { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorAnticipo { get; set; }

        [Required]
        [MaxLength(50)]
        public string Estado { get; set; } = "Solicitud";

        public Guid? AprobadorId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        [MaxLength(100)]
        public string? IdEgresoWorldOffice { get; set; }
        
        public DateTime? FechaPago { get; set; }

        // --- Relaciones de Navegación ---
        [ForeignKey("ProyectoId")]
        public Proyecto? Proyecto { get; set; }

        [ForeignKey("OrdenTrabajoId")]
        public ProyectoOrdenTrabajo? OrdenTrabajo { get; set; }

        [ForeignKey("SolicitanteId")]
        public Usuario? Solicitante { get; set; }

        [ForeignKey("AprobadorId")]
        public Usuario? Aprobador { get; set; }
    }
}