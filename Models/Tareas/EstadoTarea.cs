using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Tareas
{
    [Table("Tareas_Estados")]
    public class EstadoTarea
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Color { get; set; } = "#6B7280";

        public int Orden { get; set; } = 0;

        public bool EsEstadoInicial { get; set; } = false;

        public bool EsEstadoFinal { get; set; } = false;

        public bool Activo { get; set; } = true;

        /// <summary>Máximo de tareas en curso en esta columna (WIP). Null = sin límite.</summary>
        public int? LimiteWip { get; set; }

        // Navegación
        public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
    }
}