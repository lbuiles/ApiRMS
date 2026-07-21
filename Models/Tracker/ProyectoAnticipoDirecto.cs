using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tracker
{
    [Table("Proyectos_AnticiposDirectos")]
    public class ProyectoAnticipoDirecto
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProyectoId { get; set; }

        [Required]
        public Guid SolicitanteId { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal ValorAnticipo { get; set; }

        [Required, MaxLength(300)]
        public string Concepto { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Beneficiario { get; set; }

        [MaxLength(30)]
        public string Estado { get; set; } = "SOLICITADO";
        // SOLICITADO | APROBADO | PAGADO | RECHAZADO

        public Guid? AprobadorId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        [MaxLength(100)]
        public string? IdEgresoWorldOffice { get; set; }

        public DateTime? FechaPago { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        // Navegación
        [ForeignKey("ProyectoId")]
        public Proyecto? Proyecto { get; set; }

        [ForeignKey("SolicitanteId")]
        public Usuario? Solicitante { get; set; }

        [ForeignKey("AprobadorId")]
        public Usuario? Aprobador { get; set; }
    }
}