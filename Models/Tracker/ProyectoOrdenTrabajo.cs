using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tracker
{
    [Table("Proyectos_OrdenesTrabajo")]
    public class ProyectoOrdenTrabajo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProyectoId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Consecutivo { get; set; } = string.Empty;

        // --- CAMBIO: Ya no es [Required]. Ahora es opcional (?) ---
        [MaxLength(200)]
        public string? ContratistaNombre { get; set; }

        [MaxLength(50)]
        public string? ContratistaNit { get; set; }

        // --- NUEVO: Campo para asignar a alguien de nómina ---
        public Guid? TecnicoInternoId { get; set; }

        public DateTime FechaEmision { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorTotal { get; set; }

        [Required]
        [MaxLength(50)]
        public string Estado { get; set; } = "Borrador";

        [Required]
        public string AlcanceServicio { get; set; } = string.Empty;

        [Required]
        public Guid CreadorId { get; set; }

        public bool FirmaContratista { get; set; } = false;

        // --- Relaciones de Navegación ---
        [ForeignKey("ProyectoId")]
        public Proyecto? Proyecto { get; set; }

        [ForeignKey("CreadorId")]
        public Usuario? Creador { get; set; }

        // --- NUEVO: Relación de navegación con la tabla de Usuarios ---
        [ForeignKey("TecnicoInternoId")]
        public Usuario? TecnicoInterno { get; set; }

        // Una OT puede tener varios anticipos/pagos
        public List<ProyectoAnticipo> Anticipos { get; set; } = new List<ProyectoAnticipo>();
    }
}