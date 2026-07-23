using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tareas
{
    [Table("Tareas_Comentarios")]
    public class TareaComentario
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TareaId { get; set; }

        [Required]
        public Guid UsuarioId { get; set; }

        [Required]
        public string Comentario { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // --- Relaciones de Navegación ---
        [ForeignKey("TareaId")]
        public Tarea? Tarea { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }
    }
}