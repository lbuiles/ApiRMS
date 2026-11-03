using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tracker
{
    [Table("Proyectos_CortesFacturacion")]
    public class ProyectoCorteFacturacion
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProyectoId { get; set; }

        [Required]
        public Guid RegistradoPorId { get; set; }

        public DateTime FechaCorte { get; set; } = DateTime.UtcNow;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal ValorCorte { get; set; }

        [MaxLength(100)]
        public string? NumeroFactura { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        [MaxLength(30)]
        public string Estado { get; set; } = "REGISTRADO";
        // REGISTRADO | ANULADO

        // Navegación
        [ForeignKey("ProyectoId")]
        public Proyecto? Proyecto { get; set; }

        [ForeignKey("RegistradoPorId")]
        public Usuario? RegistradoPor { get; set; }
    }
}
