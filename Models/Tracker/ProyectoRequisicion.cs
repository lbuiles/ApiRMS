using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tracker
{
    [Table("Proyectos_Requisiciones")]
    public class ProyectoRequisicion
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProyectoId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Consecutivo { get; set; } = string.Empty;

        [Required]
        public Guid SolicitanteId { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string Estado { get; set; } = "Por Aprobar Supervisor";

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorEstimado { get; set; } = 0;

        public Guid? AprobadorId { get; set; }
        public DateTime? FechaAprobacion { get; set; }

        public string? Observaciones { get; set; }

        // --- Fase: En Compras -> Por Entregar ---
        // Coord. Compras adjunta la OC y registra el valor real de compra
        [MaxLength(100)]
        public string? NumeroOCCompra { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ValorRealCompra { get; set; }

        public Guid? CompradoPorId { get; set; }
        public DateTime? FechaCompra { get; set; }

        // --- Fase: Por Entregar -> CERRADA ---
        // Técnico / Almacén confirma recibo físico del material
        public Guid? RecibidoPorId { get; set; }
        public DateTime? FechaRecibo { get; set; }
        public string? ObservacionesRecibo { get; set; }

        // --- Relaciones de Navegación ---
        [ForeignKey("ProyectoId")]
        public Proyecto? Proyecto { get; set; }

        [ForeignKey("SolicitanteId")]
        public Usuario? Solicitante { get; set; }

        [ForeignKey("AprobadorId")]
        public Usuario? Aprobador { get; set; }

        [ForeignKey("CompradoPorId")]
        public Usuario? CompradoPor { get; set; }

        [ForeignKey("RecibidoPorId")]
        public Usuario? RecibidoPor { get; set; }
    }
}