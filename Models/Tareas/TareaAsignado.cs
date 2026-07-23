using System;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tareas
{
    [Table("Tareas_Asignados")]
    public class TareaAsignado
    {
        public Guid TareaId { get; set; }

        public Guid UsuarioId { get; set; }

        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;

        public Guid? AsignadoPorId { get; set; }

        // --- Relaciones de Navegación ---
        [ForeignKey("TareaId")]
        public Tarea Tarea { get; set; } = null!;

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; } = null!;

        [ForeignKey("AsignadoPorId")]
        public Usuario? AsignadoPor { get; set; }
    }
}