using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Catalogos;

namespace RmsErp.Api.Models.Clientes
{
    [Table("Clientes_Operaciones")]
    public class ClienteOperacion
    {
        [Key] public Guid Id { get; set; }
        [Required] public Guid ClienteId { get; set; }

        public int? PrioridadId { get; set; }
        public int SlaDias { get; set; } = 0;

        // Navegación
        [ForeignKey("ClienteId")] public virtual Cliente? Cliente { get; set; }
        [ForeignKey("PrioridadId")] public virtual Prioridad? Prioridad { get; set; }
    }
}