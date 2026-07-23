using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tareas
{
    [Table("Tareas_HistorialEstados")]
    public class TareaHistorialEstado
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TareaId { get; set; }

        // NULL cuando es la creación inicial de la tarea
        public int? EstadoAnteriorId { get; set; }

        [Required]
        public int EstadoNuevoId { get; set; }

        [Required]
        public Guid CambiadoPorId { get; set; }

        public DateTime FechaCambio { get; set; } = DateTime.UtcNow;

        // --- Relaciones de Navegación ---
        [ForeignKey("TareaId")]
        public Tarea? Tarea { get; set; }

        [ForeignKey("EstadoAnteriorId")]
        public EstadoTarea? EstadoAnterior { get; set; }

        [ForeignKey("EstadoNuevoId")]
        public EstadoTarea? EstadoNuevo { get; set; }

        [ForeignKey("CambiadoPorId")]
        public Usuario? CambiadoPor { get; set; }
    }
}