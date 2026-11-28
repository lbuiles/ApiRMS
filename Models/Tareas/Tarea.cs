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

        /// <summary>Número consecutivo visible (RMS-1001...). Lo asigna la base con la secuencia dbo.TareaConsecutivo.</summary>
        public int Consecutivo { get; set; }

        [Required]
        [MaxLength(250)]
        public string Titulo { get; set; } = string.Empty;

        /// <summary>Critica | Alta | Media | Baja</summary>
        [Required]
        [MaxLength(20)]
        public string Prioridad { get; set; } = "Media";

        /// <summary>Etiquetas separadas por coma, sin '#' (p. ej. "Finanzas,O&amp;M").</summary>
        [MaxLength(400)]
        public string? Etiquetas { get; set; }

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
        public ICollection<TareaPaso> Pasos { get; set; } = new List<TareaPaso>();
    }
}