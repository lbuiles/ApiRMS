using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tareas
{
    [Table("Tareas")]
    public class Tarea
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(250)]
        public string Titulo { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        [Required]
        public int EstadoId { get; set; }

        // Fechas presupuestadas (planificación)
        public DateTime? FechaPresupuestoInicio { get; set; }
        public DateTime? FechaPresupuestoFin { get; set; }

        // Fecha real en que se completó
        public DateTime? FechaRealFinalizacion { get; set; }

        // Lunes de la semana a la que pertenece la tarea
        [Required]
        [Column(TypeName = "date")]
        public DateTime SemanaProgramada { get; set; }

        // Contador de veces que se movió a otra semana
        public int VecesMovida { get; set; } = 0;

        [Required]
        public Guid CreadoPorId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaActualizacion { get; set; }

        // --- Relaciones de Navegación ---
        [ForeignKey("EstadoId")]
        public EstadoTarea? Estado { get; set; }

        [ForeignKey("CreadoPorId")]
        public Usuario? CreadoPor { get; set; }

        public ICollection<TareaAsignado> Asignados { get; set; } = new List<TareaAsignado>();
        public ICollection<TareaComentario> Comentarios { get; set; } = new List<TareaComentario>();
        public ICollection<TareaMovimiento> Movimientos { get; set; } = new List<TareaMovimiento>();
        public ICollection<TareaHistorialEstado> HistorialEstados { get; set; } = new List<TareaHistorialEstado>();
    }
}