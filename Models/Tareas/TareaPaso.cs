using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Tareas
{
    /// <summary>Paso (ítem de lista de chequeo) de una tarea; da el avance "3/5" en el tablero.</summary>
    [Table("Tareas_Pasos")]
    public class TareaPaso
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid TareaId { get; set; }

        [Required]
        [MaxLength(300)]
        public string Texto { get; set; } = string.Empty;

        public bool Completado { get; set; } = false;

        public int Orden { get; set; } = 0;

        [ForeignKey("TareaId")]
        public Tarea? Tarea { get; set; }
    }
}
