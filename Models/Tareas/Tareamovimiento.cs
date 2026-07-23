using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tareas
{
    [Table("Tareas_Movimientos")]
    public class TareaMovimiento
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TareaId { get; set; }

        // Lunes de la semana origen
        [Required]
        [Column(TypeName = "date")]
        public DateTime SemanaOrigen { get; set; }

        // Lunes de la semana destino
        [Required]
        [Column(TypeName = "date")]
        public DateTime SemanaDestino { get; set; }

        [Required]
        public Guid MovidoPorId { get; set; }

        public DateTime FechaMovimiento { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Motivo { get; set; }

        // --- Relaciones de Navegación ---
        [ForeignKey("TareaId")]
        public Tarea? Tarea { get; set; }

        [ForeignKey("MovidoPorId")]
        public Usuario? MovidoPor { get; set; }
    }
}